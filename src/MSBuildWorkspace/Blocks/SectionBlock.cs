// Decompiled with JetBrains decompiler
// Type: Microsoft.CodeAnalysis.MSBuild.SectionBlock
// Assembly: Microsoft.CodeAnalysis.Workspaces.Desktop, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: D215115A-535F-4F97-A96F-CBBE58E1FDB0
// Assembly location: SourceBrowser\bin\Microsoft.CodeAnalysis.Workspaces.Desktop.dll

using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Microsoft.CodeAnalysis.MSBuild
{
  // internal 
  public sealed class SectionBlock
  {
    private readonly string _type;
    private readonly string _parenthesizedName;
    private readonly string _value;
    private readonly IEnumerable<KeyValuePair<string, string>> _keyValuePairs;

    public string Type
    {
      get
      {
        return this._type;
      }
    }

    public string ParenthesizedName
    {
      get
      {
        return this._parenthesizedName;
      }
    }

    public string Value
    {
      get
      {
        return this._value;
      }
    }

    public IEnumerable<KeyValuePair<string, string>> KeyValuePairs
    {
      get
      {
        return this._keyValuePairs;
      }
    }

    public SectionBlock(string type, string parenthesizedName, string value, IEnumerable<KeyValuePair<string, string>> keyValuePairs)
    {
      if (string.IsNullOrEmpty(type))
        throw new ArgumentException(string.Format("StringIsNullOrEmpty type error", (object) "type"));
      if (string.IsNullOrEmpty(parenthesizedName))
        throw new ArgumentException(string.Format("StringIsNullOrEmpty type error", (object) "parenthesizedName"));
      if (string.IsNullOrEmpty(value))
        throw new ArgumentException(string.Format("StringIsNullOrEmpty type error", (object) "value"));
      this._type = type;
      this._parenthesizedName = parenthesizedName;
      this._value = value;
      this._keyValuePairs = (IEnumerable<KeyValuePair<string, string>>) Enumerable.ToList<KeyValuePair<string, string>>(keyValuePairs).AsReadOnly();
    }

    internal string GetText(int indent)
    {
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append('\t', indent);
      stringBuilder.AppendFormat("{0}({1}) = ", (object) this.Type, (object) this.ParenthesizedName);
      stringBuilder.AppendLine(this.Value);
      foreach (KeyValuePair<string, string> keyValuePair in this.KeyValuePairs)
      {
        stringBuilder.Append('\t', indent + 1);
        stringBuilder.Append(keyValuePair.Key);
        stringBuilder.Append(" = ");
        stringBuilder.AppendLine(keyValuePair.Value);
      }
      stringBuilder.Append('\t', indent);
      stringBuilder.AppendFormat("End{0}", (object) this.Type);
      stringBuilder.AppendLine();
      return ((object) stringBuilder).ToString();
    }

    internal static SectionBlock Parse(TextReader reader)
    {
      string line1;
      while ((line1 = reader.ReadLine()) != null)
      {
        line1 = line1.TrimStart((char[]) null);
        if (line1 != string.Empty)
          break;
      }
      LineScanner lineScanner1 = new LineScanner(line1);
      string delimiter1 = "(";
      string type = lineScanner1.ReadUpToAndEat(delimiter1);
      string delimiter2 = ") = ";
      string parenthesizedName = lineScanner1.ReadUpToAndEat(delimiter2);
      string str1 = lineScanner1.ReadRest();
      List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();
      string str2;
      while ((str2 = reader.ReadLine()) != null)
      {
        string line2 = str2.TrimStart((char[]) null);
        if (!(line2 == string.Empty))
        {
          if (!(line2 == "End" + type))
          {
            LineScanner lineScanner2 = new LineScanner(line2);
            string delimiter3 = " = ";
            string key = lineScanner2.ReadUpToAndEat(delimiter3);
            string str3 = lineScanner2.ReadRest();
            list.Add(new KeyValuePair<string, string>(key, str3));
          }
          else
            break;
        }
      }
      return new SectionBlock(type, parenthesizedName, str1, (IEnumerable<KeyValuePair<string, string>>) list);
    }
  }


  internal class LineScanner
  {
      private readonly string _line;
      private int _currentPosition;

      public LineScanner(string line)
      {
          _line = line;
      }

      public string ReadUpToAndEat(string delimiter)
      {
          int index = _line.IndexOf(delimiter, _currentPosition, StringComparison.Ordinal);

          if (index == -1)
          {
              return ReadRest();
          }
          else
          {
              var upToDelimiter = _line.Substring(_currentPosition, index - _currentPosition);
              _currentPosition = index + delimiter.Length;
              return upToDelimiter;
          }
      }

      public string ReadRest()
      {
          var rest = _line.Substring(_currentPosition);
          _currentPosition = _line.Length;
          return rest;
      }
  }

}

