﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Khronos_PMS.Model;
using Khronos_PMS.Util;
using BrightIdeasSoftware;

namespace Khronos_PMS.View {
    public partial class MainForm : Form {
        private User user;

        public MainForm(String username, int userID) {
            InitializeComponent();
            this.user = ProjectManager.entities.Users.Find(userID);

            if (user != null)
                userLabel.Text = username;

            unitsTreeView.CanExpandGetter = u => (u as Unit).HasChildren;
            unitsTreeView.ChildrenGetter = u => (u as Unit).Children;

            int statusOffset = 7, priorityOffset = 11;

            workersListView.CheckStateGetter = w => (w as Worker).AssignedToProject((Project) projectsListView.SelectedObject) ? CheckState.Checked : CheckState.Unchecked;

            workersListView.CheckStatePutter = (w, value) => {
                switch (value) {
                    case CheckState.Checked:
                        ProjectManager.AssignWorker((Project) projectsListView.SelectedObject, (Worker) w);
                        return CheckState.Unchecked;
                    case CheckState.Unchecked:
                        ProjectManager.UnassignWorker((Project) projectsListView.SelectedObject, (Worker) w);
                        return CheckState.Checked;
                    default:
                        return CheckState.Unchecked;
                }
            };
            projectsListView.GetColumn(0).ImageGetter = i => 0;
            workersListView.GetColumn(0).ImageGetter = w => 1;
            unitsTreeView.GetColumn(0).ImageGetter = u => (u as Unit).Status + 2;
            unitsTreeView.GetColumn(1).ImageGetter = u => (u as Unit).Status + statusOffset;
            unitsTreeView.GetColumn(1).AspectToStringConverter = s => StatusManager.Name(StatusManager.getStausById((int) s));
            unitsTreeView.GetColumn(2).ImageGetter = u => (u as Unit).Priority + priorityOffset;
            unitsTreeView.GetColumn(2).AspectToStringConverter = s => PriorityManager.Name(PriorityManager.GetPriorityById((int) s));
            unitsTreeView.GetColumn(3).ImageGetter = u => 6;
            unitsTreeView.GetColumn(3).AspectToStringConverter = d => ((DateTime) d).ToShortDateString();
            unitsTreeView.GetColumn(4).ImageGetter = u => 17;
            unitsTreeView.GetColumn(5).ImageGetter = u => 17;
            assigneesListView.GetColumn(0).ImageGetter = a => 0;
            rightTableLayout.ColumnStyles[1].Width = 0;

            describedTaskRenderer.TitleFont = new Font(label1.Font, FontStyle.Bold);
            describedTaskRenderer.DescriptionAspectName = "Note";
            describedTaskRenderer.ImageList = activityImageList;
            describedTaskRenderer.TitleDescriptionSpace = 6;
            activityListView.GetColumn(0).ImageGetter = a => 0;
        }

        private void MainForm_Load(Object sender, EventArgs e) {
            List<Project> projects = ProjectManager.GetProjects(user);
            setRole(projects[0]);
            SetVisibility();
            projectsListView.DataSource = projects;
        }

        private void button1_Click(Object sender, EventArgs e) {
            rightTableLayout.ColumnStyles[1].Width = 0;
        }

        private void projectsSearchButton_Click(Object sender, EventArgs e) {
            searchProjects();
        }

        private void workersSearchButton_Click(Object sender, EventArgs e) {
            searchWorkers();
        }

        private void unitsSearchButton_Click(Object sender, EventArgs e) {
            searchUnits();
        }

        private void projectToolStripMenuItem_Click(Object sender, EventArgs e) {
            Status status = (Status) ((ToolStripItem) sender).Tag;
            projectStatusMenuButton.Image = StatusManager.Image(status);

            Project selectedProject = (Project) projectsListView.SelectedObject;
            new Task(() => { StatusManager.UpdateStatus(selectedProject, status); }).Start();
        }

        private void unitStatusToolStripMenuItem_Click(Object sender, EventArgs e) {
            Status status = (Status) ((ToolStripItem) sender).Tag;
            unitStatusMenuButton.Image = StatusManager.Image(status);

            Unit selectedUnit = (Unit) unitsTreeView.SelectedObject;
            StatusManager.UpdateStatus(selectedUnit, status);
        }

        private void unitPriorityToolStripMenuItem_Click(Object sender, EventArgs e) {
            Priority priority = (Priority) ((ToolStripItem) sender).Tag;
            unitPriorityMenuButton.Image = PriorityManager.Image(priority);

            Unit selectedUnit = (Unit) unitsTreeView.SelectedObject;
            PriorityManager.UpdatePriority(selectedUnit, priority);
        }

        private void projectsListView_SelectionChanged(Object sender, EventArgs e) {
            Project selectedProject = (Project) projectsListView.SelectedObject;
            if (selectedProject == null) return;
            projectNameLabel.Text = selectedProject.Name;
            projectDescriptionLabel.Text = selectedProject.Description;
            startDateLabel.Text = selectedProject.StartDate.ToShortDateString();
            //Showing end date if project is completed elese deadline
            setDates(selectedProject);
            budgetLabel.Text = String.Format("{0:0.00} KM", selectedProject.Budget);
            expenseLabel.Text = String.Format("{0:0.00} KM", selectedProject.Expense);
            bossNameLabel.Text = selectedProject.Boss.FullName;
            projectStatusMenuButton.Image = StatusManager.Image(StatusManager.getStausById(selectedProject.Status));
            setRole(selectedProject);

            SetVisibility();

            unitsTreeView.Roots = new List<Unit>(1);

            new Task(() => {
                try {
                    List<Worker> workers = ProjectManager.GetWorkers(selectedProject, user);
                    workersListView.SetObjects(workers);

                    List<Unit> u = ProjectManager.GetRootUnits(selectedProject);
                    unitsTreeView.SetObjects(u);
                } catch (Exception ex) {
                    Console.Out.WriteLine(ex.StackTrace);
                }
            }).Start();
        }

        private void setDates(Project selectedProject) {
            //Showing end date if project is completed elese deadline
            if (StatusManager.getStausById(selectedProject.Status) == Status.COMPLETED) {
                label3.Text = "End date:";
                endDateLabel.Text = Convert.ToDateTime(selectedProject.EndDate).ToShortDateString();
            } else {
                label3.Text = "Deadline:";
                endDateLabel.Text = selectedProject.DeadlineDate.ToShortDateString();
            }
        }

        private void projectsSearchTextbox_TextChanged(object sender, EventArgs e) {
            searchProjects();
        }

        private void searchProjects() {
            projectsListView.UseFiltering = true;
            projectsListView.ModelFilter = new ModelFilter(x => {
                var myProject = x as Project;
                return x != null && (myProject.Name.ToLower().Contains(projectsSearchTextbox.Text.ToLower()));
            });
        }

        private void searchWorkers() {
            workersListView.UseFiltering = true;
            workersListView.ModelFilter = new ModelFilter(x => {
                var worker = x as Worker;
                return x != null && (worker.FirstName.ToLower().Contains(workersSearchTextBox.Text.ToLower()));
            });
        }

        private void setRole(Project selectedProject) {
            if (user != null) {
                if (selectedProject.BossID == user.ID) {
                    projectRoleLabel.Text = Role.Boss.ToString();
                    projectRoleLabel.Tag = Role.Boss;
                } else if (selectedProject.SupervisorID == user.ID) {
                    projectRoleLabel.Text = Role.Supervisor.ToString();
                    projectRoleLabel.Tag = Role.Supervisor;
                } else {
                    bool roleSet = false;
                    foreach (var item in selectedProject.Customers) {
                        if (item.ID == user.ID) {
                            projectRoleLabel.Text = Role.Customer.ToString();
                            projectRoleLabel.Tag = Role.Customer;
                            roleSet = true;
                        }
                    }
                    foreach (var item in selectedProject.AssignedWorkers) {
                        if (item.WorkerID == user.ID) {
                            projectRoleLabel.Text = Role.Worker.ToString();
                            projectRoleLabel.Tag = Role.Worker;
                            roleSet = true;
                        }
                    }
                    if (roleSet != true) {
                        projectRoleLabel.Text = "Unknown";
                    }
                }
            } else {
                projectRoleLabel.Text = "Unknown";
            }
        }

        private void workersSearchTextBox_TextChanged(object sender, EventArgs e) {
            searchWorkers();
        }

        private void unitsTreeView_SelectionChanged(Object sender, EventArgs e) {
            Unit unit = (Unit) unitsTreeView.SelectedObject;
            if (unit == null)
                rightTableLayout.ColumnStyles[1].Width = 0;
            else {
                unitStatusMenuButton.Image = StatusManager.Image(StatusManager.getStausById((int) unit.Status));
                unitPriorityMenuButton.Image = PriorityManager.Image(PriorityManager.GetPriorityById(unit.Priority));
                unitNameLabel.Text = unit.Name;
                unitEstimatedManhoursLabel.Text = unit.EstManhours + " h";
                unitSpentManhoursLabel.Text = unit.SpentManhours + " h";
                unitDueDateLabel.Text = unit.DueDate.ToShortDateString();
                unitExpenseLabel.Text = string.Format("{0:0.00} KM", unit.Expense);
                assigneesListView.DataSource = new List<Worker>(1);
                new Task(() => { assigneesListView.SetObjects(unit.Assignees); }).Start();
                activitiesTableLayout.Hide();
                rightTableLayout.ColumnStyles[1].Width = 315;
            }
        }

        private void unitToolStripMenuItem_Click(Object sender, EventArgs e) {
            addNewUnitButton.PerformClick();
        }

        private void exitToolStripMenuItem_Click(Object sender, EventArgs e) {
            Close();
        }

        private void logOutToolStripMenuItem_Click(Object sender, EventArgs e) {
            Application.Restart();
        }

        private void searchUnitsTextBox_TextChanged(Object sender, EventArgs e) {
            searchUnits();
        }

        private void searchUnits() {
            unitsTreeView.UseFiltering = true;
            unitsTreeView.ModelFilter = new ModelFilter(x => {
                var unit = x as Unit;
                return (x as Unit) != null && unit.Name.ToLower().Contains(unitsSearchTextBox.Text.ToLower());
            });
        }

        private void addNewUnitButton_Click(Object sender, EventArgs e) {
            UnitForm unitForm = new UnitForm((Project) projectsListView.SelectedObject, promptTextBox3.Text, (Unit) unitsTreeView.SelectedObject);
            DialogResult result = unitForm.ShowDialog();
            if (result == DialogResult.OK) {
                Unit u = unitForm.getRootUnit();
                if (u.IsRoot) {
                    unitsTreeView.AddObject(u);
                }
            }
        }

        private void financialReportToolStripMenuItem_Click(object sender, EventArgs e) {
            FinancialReportForm fr = new FinancialReportForm((Project) projectsListView.SelectedObject);
            fr.ShowDialog();
        }

        private void projectStatusToolStripMenuItem_Click(object sender, EventArgs e) {
            ProjectReportForm pr = new ProjectReportForm((Project) projectsListView.SelectedObject);
            pr.ShowDialog();
        }

        private void activitiesReportToolStripMenuItem_Click(object sender, EventArgs e) {
            ActivitiesReportForm ar = new ActivitiesReportForm((Project) projectsListView.SelectedObject);
            ar.ShowDialog();
        }

        private void addActivityToolstripMenuItem_Click(object sender, EventArgs e) {
            Unit unit = (Unit) unitsTreeView.SelectedObject;
            ActivityForm activityForm = new ActivityForm(unit, user, null);
            activityForm.ShowDialog();
            if (activityForm.DialogResult == DialogResult.OK) {
                activityListView.SetObjects(UnitManager.GetActivities(unit, user));
            }
            //UpdateValues(unit);
        }

        private void viewAllToolStripMenuItem_Click(object sender, EventArgs e) {
            Unit unit = (Unit) unitsTreeView.SelectedObject;
            List<Activity> activities = UnitManager.GetActivities(unit, user);
            activityListView.SetObjects(activities);
            activitiesTableLayout.Show();
        }

        private void unitEditButton_Click(Object sender, EventArgs e) {
            UnitForm unitform = new UnitForm((Unit) unitsTreeView.SelectedObject, (Project) projectsListView.SelectedObject);
            unitform.ShowDialog();
        }

        private void activityToolStripMenuItem_Click(object sender, EventArgs e) {
            if (unitsTreeView.SelectedIndex != -1) {
                addActivityToolstripMenuItem.PerformClick();
            } else {
                MessageBox.Show("You must select one unit!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void editActivityButton_Click(object sender, EventArgs e) {
            if (activityListView.SelectedIndex != -1) {
                new ActivityForm((Unit) unitsTreeView.SelectedObject, user, (Activity) activityListView.SelectedObject).ShowDialog();
            } else {
                MessageBox.Show("You must select one activity!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetVisibility() {
            switch ((Role) projectRoleLabel.Tag) {
                case Role.Boss:
                    SetVisibilityForBoss();
                    break;
                case Role.Worker:
                    SetVisibilityForWorker();
                    break;
                case Role.Supervisor:
                    SetVisibilityForSupervisor();
                    break;
                case Role.Customer:
                    SetVisibilityForCustomer();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void SetVisibilityForWorker() {
            unitToolStripMenuItem.Visible = false;
            reportsToolStripMenuItem.Visible = false;
            workersListView.CheckBoxes = false;
            workersListView.HeaderStyle = ColumnHeaderStyle.None;
            projectStatusMenuButton.Enabled = false;
            unitsTableLayout.RowStyles[0].Height = 0;
            unitEditButton.Visible = false;
            unitInfoTableLayout.RowStyles[3].Height = 0;
            editActivityButton.Visible = false;
            projectInfoTableLayout.Hide();
            addActivityToolstripMenuItem.Visible = true;
        }

        private void SetVisibilityForBoss() {
            unitToolStripMenuItem.Visible = true;
            reportsToolStripMenuItem.Visible = true;
            workersListView.CheckBoxes = true;
            workersListView.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            projectStatusMenuButton.Enabled = true;
            unitsTableLayout.RowStyles[0].Height = 30;
            unitEditButton.Visible = true;
            unitInfoTableLayout.RowStyles[3].Height = 25;
            editActivityButton.Visible = true;
            projectInfoTableLayout.Show();
            addActivityToolstripMenuItem.Visible = false;
        }

        private void SetVisibilityForSupervisor() {
            unitToolStripMenuItem.Visible = false;
            reportsToolStripMenuItem.Visible = true;
            workersListView.CheckBoxes = false;
            workersListView.HeaderStyle = ColumnHeaderStyle.None;
            projectStatusMenuButton.Enabled = false;
            unitsTableLayout.RowStyles[0].Height = 0;
            unitEditButton.Visible = false;
            unitInfoTableLayout.RowStyles[3].Height = 25;
            editActivityButton.Visible = false;
            projectInfoTableLayout.Show();
            addActivityToolstripMenuItem.Visible = false;
        }

        private void SetVisibilityForCustomer() {
            unitToolStripMenuItem.Visible = false;
            reportsToolStripMenuItem.Visible = true;
            workersListView.CheckBoxes = false;
            workersListView.HeaderStyle = ColumnHeaderStyle.None;
            projectStatusMenuButton.Enabled = false;
            unitsTableLayout.RowStyles[0].Height = 0;
            unitEditButton.Visible = false;
            unitInfoTableLayout.RowStyles[3].Height = 0;
            editActivityButton.Visible = false;
            activitiesToolStripSplitButton.Visible = false;
            projectInfoTableLayout.Hide();
            addActivityToolstripMenuItem.Visible = false;
        }

        private void viewActivityButton_Click(Object sender, EventArgs e) {
            if (activityListView.SelectedIndex != -1) {
                new ActivityViewForm((Activity) activityListView.SelectedObject).ShowDialog();
            } else {
                MessageBox.Show("You must select one activity!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e) {
            new AboutMessageBox();
        }

        private void UpdateValues(Unit unit) {
            unitSpentManhoursLabel.Text = String.Format("{0:0.00} h", unit.SpentManhours);
            unitExpenseLabel.Text = String.Format("{0:0.00} KM", unit.Expense);
        }
    }
}