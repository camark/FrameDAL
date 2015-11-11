﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FrameDAL.Attributes;

namespace FrameTest
{
    [Table("account")]
    public class Account
    {
        [Column("user_id")]
        public string UserId { get; set; }

        [Id(GeneratorType.Uuid)]
        [Column("id")]
        public string Id { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("password")]
        public string Password { get; set; }

        [Column("balance")]
        public int? Balance { get; set; }
    }
}
