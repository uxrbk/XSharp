﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Spruce;

namespace XSharp.Tokens {
  public class Root : Spruce.Tokens.Token {
    public Root(Type aEmitterType) : base(null) {
      // Load emitters to pattern list
      foreach (var xMethod in typeof(Emitters).GetRuntimeMethods()) {
        var xAttrib = xMethod.GetCustomAttribute<Spruce.Attribs.Emitter>();
        if (xAttrib != null) {
          AddPattern(EmitterHandler(aEmitterType, xMethod), xAttrib.TokenTypes);
        }
      }
    }

    protected Action<object, List<CodePoint>> EmitterHandler(Type aEmitterType, MethodInfo aMethod) {
      return (object aCompiler, List<CodePoint> aPoints) => {
        var xEmitter = Activator.CreateInstance(aEmitterType, aCompiler, aPoints);
        string xResult;
        if (aMethod.GetParameters().Length == 0) {
          // Using this method, users must read CodePoints directly
          xResult = (string) aMethod.Invoke(xEmitter, null);
        } else {
          xResult = (string) aMethod.Invoke(xEmitter, aPoints.Select(q => q.Value).ToArray());
        }
        ((Compiler) aCompiler).WriteLine(xResult);
      };
    }

    protected override bool IsMatch(ref object rValue) {
      return false;
    }

    public List<CodePoint> Parse(string aText) {
      // Important for end detection. Do not TrimStart, will goof up CodePoint indexes.
      aText = aText.TrimEnd();
      var xResult = new List<CodePoint>();
      int aPos = 0;
      Spruce.Tokens.Token xToken = this;

      while (aPos < aText.Length) {
        var xCP = xToken.Next(aText, ref aPos);
        if (xCP == null) {
          break;
        }
        xToken = xCP.Token;
        xResult.Add(xCP);
      }

      return xResult;
    }

  }
}
