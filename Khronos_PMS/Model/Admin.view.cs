﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Khronos_PMS.Model {
    public partial class Admin : User {
        public override string GetName() {
            return this.FirstName + " " + this.LastName;
        }
    }
}
