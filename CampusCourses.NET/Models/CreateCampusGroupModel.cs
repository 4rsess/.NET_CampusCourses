﻿using System.ComponentModel.DataAnnotations;

namespace CampusCourses.NET.Models
{
    public class CreateCampusGroupModel
    {
        [MinLength(1), Required]
        public string name { get; set; }
    }
}
