﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Ardent_API.Models
{
    public class User
    {
        private User() { }

        public static User Create(string username, string passwordHash, int role)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(passwordHash) || (role < 0 && role > 2))
                throw new ArgumentException();

            return new User
            {
                Id = Guid.NewGuid(),
                Username = username,
                PasswordHash = passwordHash,
                Role = role
            };
        }

        [Required]
        public Guid Id { get; set; }

        [Required]
        [StringLength(10, ErrorMessage = "Username length can't be more than 10.")]
        public string Username { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        [Range(0, 2)]
        public int Role { get; set; }
        // 0 = admin, 1 = designer, 2 = client

        public virtual ICollection<Project> Projects { get; set; }
    }
}