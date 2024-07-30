﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace API.FurnitureStore.Shared.DTOs
{
    public class UserRegsitrationDto
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public string EmailAdress { get; set; }
        [Required]
        public string Password { get; set; }
    }
}
