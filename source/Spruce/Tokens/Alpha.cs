﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Spruce.Tokens {
    public class Alpha : Token {
        public Alpha() : base(Chars.Alpha) { }

        protected override object Check(string aText) {
            throw new NotImplementedException();
        }
    }
}