﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.DTO
{
    public class ConfigurationDTO :AbstractDTO
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
