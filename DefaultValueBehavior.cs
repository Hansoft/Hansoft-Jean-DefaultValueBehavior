using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

using HPMSdk;

using Hansoft.Jean.Behavior;
using Hansoft.ObjectWrapper;

namespace Hansoft.Jean.Behavior.DefaultValueBehavior
{
    public class DefaultValueBehavior : AbstractBehavior
    {
        // TODO: Consider to add default (or simply a hash with additional attributes), perhaps by sending in a string array of attributes
        // to the general ColumnDefinition class (to be...) in the behavior namespace.
        class ColumnDefault
        {
            private bool isCustomColumn;
            private string defaultValue;
            private string customColumnName;
            private HPMProjectCustomColumnsColumn customColumn;
            private EHPMProjectDefaultColumn defaultColumnType;

            internal ColumnDefault(bool isCustomColumn, string customColumnName, EHPMProjectDefaultColumn defaultColumnType, string defaultValue )
            {
                this.isCustomColumn = isCustomColumn;
                this.customColumnName = customColumnName;
                this.defaultColumnType = defaultColumnType;
                this.defaultValue = defaultValue;
            }

            internal void Initialize(ProjectView projectView)
            {
                if (isCustomColumn)
                {
                    customColumn = projectView.GetCustomColumn(customColumnName);
                    if (customColumn == null)
                        throw new ArgumentException("Could not find custom column:" + customColumnName);
                }
            }

            internal void Apply(Task task)
            {
                if (isCustomColumn)
                    task.SetCustomColumnValue(customColumn, defaultValue);
                else
                {
                    task.SetDefaultColumnValue(defaultColumnType, defaultValue);
                }
            }
        }

        private string projectName;
        private EHPMReportViewType viewType;
        private Project project;
        private ProjectView projectView;
        private List<ColumnDefault> columnDefaults;
        string title;

        public DefaultValueBehavior(XmlElement configuration)
            : base(configuration) 
        {
            projectName = GetParameter("HansoftProject");
            columnDefaults = GetColumnDefaults(configuration);
            viewType = GetViewType(GetParameter("View"));
            title = "DefaultValueBehavior: " + configuration.InnerText;
        }

        public override string Title
        {
            get { return title; }
        }

        // TODO: Subject to refactoting
        private List<ColumnDefault> GetColumnDefaults(XmlElement parent)
        {
            List<ColumnDefault> columnDefaults = new List<ColumnDefault>();
            foreach (XmlNode node in parent.ChildNodes)
            {
                if (node is XmlElement)
                {
                    XmlElement el = (XmlElement)node;
                    switch (el.Name)
                    {
                        case ("CustomColumn"):
                            columnDefaults.Add(new ColumnDefault(true, el.GetAttribute("Name"), EHPMProjectDefaultColumn.NewVersionOfSDKRequired, el.GetAttribute("DefaultValue")));
                            break;
                        case ("Risk"):
                            columnDefaults.Add(new ColumnDefault(false, null, EHPMProjectDefaultColumn.Risk, el.GetAttribute("DefaultValue")));
                            break;
                        case ("Priority"):
                            columnDefaults.Add(new ColumnDefault(false, null, EHPMProjectDefaultColumn.BacklogPriority, el.GetAttribute("DefaultValue")));
                            break;
                        case ("EstimatedDays"):
                            columnDefaults.Add(new ColumnDefault(false, null, EHPMProjectDefaultColumn.EstimatedIdealDays, el.GetAttribute("DefaultValue")));
                            break;
                        case ("Category"):
                            columnDefaults.Add(new ColumnDefault(false, null, EHPMProjectDefaultColumn.BacklogCategory, el.GetAttribute("DefaultValue")));
                            break;
                        case ("Points"):
                            columnDefaults.Add(new ColumnDefault(false, null, EHPMProjectDefaultColumn.ComplexityPoints, el.GetAttribute("DefaultValue")));
                            break;
                        case ("Status"):
                            columnDefaults.Add(new ColumnDefault(false, null, EHPMProjectDefaultColumn.ItemStatus, el.GetAttribute("DefaultValue")));
                            break;
                        case ("Confidence"):
                            columnDefaults.Add(new ColumnDefault(false, null, EHPMProjectDefaultColumn.Confidence, el.GetAttribute("DefaultValue")));
                            break;
                        case ("Hyperlink"):
                            columnDefaults.Add(new ColumnDefault(false, null, EHPMProjectDefaultColumn.Hyperlink, el.GetAttribute("DefaultValue")));
                            break;
                        case ("Name"):
                            columnDefaults.Add(new ColumnDefault(false, null, EHPMProjectDefaultColumn.ItemName, el.GetAttribute("DefaultValue")));
                            break;
                        case ("WorkRemaining"):
                            columnDefaults.Add(new ColumnDefault(false, null, EHPMProjectDefaultColumn.WorkRemaining, el.GetAttribute("DefaultValue")));
                            break;
                        default:
                            throw new ArgumentException("Unknown column type specified in Default Value behavior : " + el.Name);
                    }
                }
            }
            return columnDefaults;
        }

        // TODO: Subject to refactoting
        private EHPMReportViewType GetViewType(string viewType)
        {
            switch (viewType)
            {
                case ("Agile"):
                    return EHPMReportViewType.AgileMainProject;
                case ("Scheduled"):
                    return EHPMReportViewType.ScheduleMainProject;
                case ("Bugs"):
                    return EHPMReportViewType.AllBugsInProject;
                case ("Backlog"):
                    return EHPMReportViewType.AgileBacklog;
                default:
                    throw new ArgumentException("Unsupported View Type: " + viewType);
            }
        }

        public override void Initialize()
        {
            project = HPMUtilities.FindProject(projectName);
            if (project == null)
                throw new ArgumentException("Could not find project:" + projectName);
            if (viewType == EHPMReportViewType.AgileBacklog)
                projectView = project.ProductBacklog;
            else if (viewType == EHPMReportViewType.AllBugsInProject)
                projectView = project.BugTracker;

            foreach (ColumnDefault columnDefault in columnDefaults)
                columnDefault.Initialize(projectView);
        }

        private void DoSetDefault(HPMChangeCallbackData_TaskCreateUnifiedTask[] createdTasks)
        {
            foreach (HPMChangeCallbackData_TaskCreateUnifiedTask createdTask in createdTasks)
            {
                Task task = Task.GetTask(createdTask.m_TaskRefID);

                // We should not set the default when a proxy item is created as this would overwrite the values on the underlying backlog item.
                if (!(task is ProductBacklogItemInSchedule) && !(task is ProductBacklogItemInSprint)) 
                    foreach (ColumnDefault columnDefault in columnDefaults)
                        columnDefault.Apply(task);
            }
        }

        public override void OnTaskCreate(TaskCreateEventArgs e)
        {
            if (e.Data.m_ProjectID.m_ID == projectView.Id)
                DoSetDefault(e.Data.m_Tasks);
        }
    }
}
