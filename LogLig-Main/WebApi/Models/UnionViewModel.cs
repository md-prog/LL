﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApi.Models
{
    public class UnionViewModel
    {
        public string Name { get;  set; }
        public string Description { get;  set; }
        public bool IsHandicapped { get;  set; }
        public string Logo { get;  set; }
        public string PrimaryImage { get; set; }
        public string IndexImage { get;  set; }
        public string AssociationIndexInfo { get;  set; }
        public string Address { get;  set; }
        public string Phone { get;  set; }
        public string Email { get;  set; }
    }
}