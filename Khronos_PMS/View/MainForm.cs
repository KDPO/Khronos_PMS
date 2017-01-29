﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Khronos_PMS.Model;
using Khronos_PMS.Util;
using BrightIdeasSoftware;
using Khronos_PMS.ModelView;

namespace Khronos_PMS.View {
    public partial class MainForm : Form {
        private User user;

        public MainForm(User user) {
            InitializeComponent();
            this.user = user;

            if (user != null)
                userLabel.Text = user.GetName();

            // pošto u LoginForm imamo progress bar ovdje ćemo učitati sve što treba iz baze
            // pa kada se forma pokrene sve bude učitano

            projectsListView.GetColumn(0).ImageGetter = i => 0;
            workersListView.GetColumn(0).ImageGetter = i => 1;
            List<Project> projects = ProjectManager.GetProjects(user);
            List<Worker> workers = ProjectManager.GetWorkers(projects[0]);
            List<Unit> units = ProjectManager.GetUnits(projects[0]);
            projectsListView.DataSource = projects;
            workersListView.DataSource = workers;
            unitsTreeListView.DataSource = units;
        }

        private void MainForm_Load(Object sender, EventArgs e) {
            //todo neke labele treba sakriti u zavisnoti ko se prijavio
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
            //todo Num 8. implementirati pretragu unita, prikazati obavještenja
        }

        private void projectToolStripMenuItem_Click(Object sender, EventArgs e) {
            Status status = (Status) ((ToolStripItem) sender).Tag;
            projectStatusMenuButton.Image = StatusManager.Image(status);

            new Task(() => {
                if (projectsListView.SelectedIndex != -1) {
                    Project selectedProject = (Project) projectsListView.SelectedObject;
                    StatusManager.UpdateStatus(selectedProject, status);
                }
            }).Start();
        }

        private void unitStatusToolStripMenuItem_Click(Object sender, EventArgs e) {
            Status status = (Status) ((ToolStripItem) sender).Tag;
            unitStatusMenuButton.Image = StatusManager.Image(status);

            new Task(() => {
                Unit selectedUnit = (Unit) unitsTreeView.SelectedNode.Tag;
                StatusManager.UpdateStatus(selectedUnit, status);
            }).Start();
        }

        private void unitPriorityToolStripMenuItem_Click(Object sender, EventArgs e) {
            Priority priority = (Priority) ((ToolStripItem) sender).Tag;
            unitPriorityMenuButton.Image = PriorityManager.Image(priority);

            new Task(() => {
                Unit selectedUnit = (Unit) unitsTreeView.SelectedNode.Tag;
                PriorityManager.UpdatePriority(selectedUnit, priority);
            }).Start();
        }

        private void projectsListView_SelectedIndexChanged(object sender, EventArgs e) {
            //inicijalizovati sva polja vezana za projekat (Project name, role, boss, description, dates, ...) 
            if (projectsListView.SelectedIndex != -1) {
                Project selectedProject = (Project) projectsListView.SelectedObject;
                projectNameLabel.Text = selectedProject.Name;
                projectDescriptionLabel.Text = selectedProject.Description;
                startDateLabel.Text = selectedProject.StartDate.ToShortDateString();
                endDateLabel.Text = selectedProject.DeadlineDate.ToShortDateString();
                budgetLabel.Text = selectedProject.Budget + " KM";
                expenseLabel.Text = selectedProject.Expense + " KM";
                bossNameLabel.Text = selectedProject.Boss.FullName;
                projectStatusMenuButton.Image = StatusManager.Image(StatusManager.getStausById(selectedProject.Status));
                setRole(selectedProject);

                // možda treba u background
                workersListView.DataSource = ProjectManager.GetWorkers(selectedProject);

                // postavka unita
                // TODO pozvati u thread pa invoke
                //unitsTreeView.Nodes.Add(UnitView.getRootUnits(selectedProject.ID));
                unitsTreeView.Nodes.Clear();
                List<UnitView> units = UnitView.getRootUnits(selectedProject.ID);
                foreach (UnitView unitView in units) {
                    TreeNode rootNode = new TreeNode(unitView.Name);
                    rootNode.Tag = unitView;
                    unitsTreeView.Nodes.Add(rootNode);
                    setupTreeNode(rootNode);
                }
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
                } else if (selectedProject.SupervisorID == user.ID) {
                    projectRoleLabel.Text = Role.Supervisor.ToString();
                } else {
                    bool roleSet = false;
                    foreach (var item in selectedProject.Customers) {
                        if (item.ID == user.ID) {
                            projectRoleLabel.Text = Role.Customer.ToString();
                            roleSet = true;
                        }
                    }
                    foreach (var item in selectedProject.AssignedWorkers) {
                        if (item.WorkerID == user.ID) {
                            projectRoleLabel.Text = Role.Worker.ToString();
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

        private void unitsTreeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e) {
           // TreeNode selectedNode = e.Node;
           // UnitView selectedUnit = ((UnitView)selectedNode.Tag);
           // selectedUnit.setChildrenAndGrandChildren();
           // if (selectedNode.Nodes.Count == 0)
           //     foreach (UnitView child in selectedUnit.children) {
           //         TreeNode childNode = new TreeNode(child.Name);
           //         childNode.Tag = child;
           //         selectedNode.Nodes.Add(childNode);
           //         foreach (UnitView grandChild in child.children)
           //         {
           //             TreeNode grandChildNode = new TreeNode(grandChild.Name);
           //             grandChildNode.Tag = grandChild;
           //             childNode.Nodes.Add(grandChildNode);
           //         }
           //    }
           // if (selectedNode.IsExpanded)
           //     selectedNode.Collapse();
           // else
           //     selectedNode.Expand();
        }

        private void unitsTreeView_AfterExpand(object sender, TreeViewEventArgs e)
        {
            TreeNode selectedNode = e.Node;
            setupTreeNode(selectedNode);
        }

        private void setupTreeNode(TreeNode selectedNode) {
            UnitView selectedUnit = ((UnitView)selectedNode.Tag);
            selectedUnit.setChildrenAndGrandChildren();
            if (selectedNode.Nodes.Count == 0)
                foreach (UnitView child in selectedUnit.children)
                {
                    TreeNode childNode = new TreeNode(child.Name);
                    childNode.Tag = child;
                    selectedNode.Nodes.Add(childNode);
                    foreach (UnitView grandChild in child.children)
                    {
                        TreeNode grandChildNode = new TreeNode(grandChild.Name);
                        grandChildNode.Tag = grandChild;
                        childNode.Nodes.Add(grandChildNode);
                    }

                }
        }
    }
}