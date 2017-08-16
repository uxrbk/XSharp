﻿using System;
using System.Collections.Generic;
using System.Text;

namespace XSharp.Parsers {
  public abstract class Parser {
    public static class Chars {
      public static readonly string Alpha;
      public static readonly string AlphaUpper = "ABCDEFGHIJKLMNOPQRTSUVWXYZ";
      public static readonly string AlphaLower;
      public static readonly string Number = "0123456789";
      public static readonly string AlphaNum;
      public static readonly string Symbol = "`!@#$%^&()+-*/=,.:{}[]";

      static Chars() {
        AlphaLower = AlphaUpper.ToLower();
        Alpha = AlphaUpper + AlphaLower;
        AlphaNum = Alpha + AlphaNum;
      }
    }

    // Do not store any state in this class. It is
    // used from different places at once and only exists
    // to allow overrides since .NET types have no VMT.
    public abstract object Parse(string aText, ref int rStart);
  }
}
