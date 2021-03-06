using Khronos_PMS.Model;
using Khronos_PMS.View;
using System;
using System.Windows.Forms;

namespace Khronos_PMS.Util {
    public class ActivityManager {
        private User user;
        private Unit unit;
        private Activity activity;

        public ActivityManager(Unit u, User user, int manhour, double expense, String note) {
            this.user = user;
            unit = u;
            addActivity(manhour, expense, note);
        }

        public ActivityManager(Activity activity) {
            this.activity = activity;
            try {
                ProjectManager.entities.Activities.Attach(activity);
                ProjectManager.entities.Entry(activity).State = System.Data.Entity.EntityState.Modified;
                ProjectManager.entities.SaveChangesAsync();
                string toLog = activity.ID + "#" +
                    activity.Note + "#" +
                    activity.Manhour + "#";
                LogManager.writeToLog(ProjectManager.entities, "Activity", "update", toLog, LoginManager.LoggedUser.ID);
            } catch (Exception ex) {
                Console.Out.WriteLine(ex.StackTrace);
                ProjectManager.entities.Entry(activity).State = System.Data.Entity.EntityState.Detached;
            }
        }

        private void addActivity(int manhour, double expense, String note) {
            activity = new Activity();
            try {
                activity.Manhour = manhour;
                activity.Expense = (decimal) expense;
                activity.Note = note;
                activity.WorkerID = user.ID;
                activity.Date = DateTime.Now;
                activity.ProjectID = unit.ProjectID;
                activity.UnitID = unit.ID;
                ProjectManager.entities.Activities.Add(activity);
                ProjectManager.entities.SaveChangesAsync();
                LogManager.writeToLog(ProjectManager.entities, "Activity", "insert", activity.ID.ToString(), LoginManager.LoggedUser.ID);
            } catch (Exception e) {
                Console.Write(e.StackTrace);
            }
        }

        public Activity Activity => activity;
    }
}