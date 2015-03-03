﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Latino.TextMining
{
    public enum TextFeatureOperation
    {
        Replace,
        Append,
        AppendDistinct,
        Custom
    }

    public class TextFeatureProcessor
    {
        private readonly List<TextFeature> mFeatures = new List<TextFeature>();
        private readonly List<string> mAppends = new List<string>(128);
        private readonly List<string> mDistinctAppends = new List<string>(128);

        public string Run(string text)
        {
            mAppends.Clear();
            mDistinctAppends.Clear();
            foreach (TextFeature feature in mFeatures)
            {
                switch (feature.Operation)
                {
                    case TextFeatureOperation.Replace:
                        text = feature.Regex.Replace(text, feature.MarkToken);
                        break;

                    case TextFeatureOperation.Custom:
                        text = feature.PerformCustomOperation(text);
                        break;

                    case TextFeatureOperation.Append:
                        if (feature.Regex.IsMatch(text))
                        {
                            mAppends.Add(feature.MarkToken);
                        }
                        break;

                    case TextFeatureOperation.AppendDistinct:
                        if (feature.Regex.IsMatch(text))
                        {
                            mDistinctAppends.Add(feature.MarkToken);
                        }
                        break;
                }
            }
            if (mAppends.Any())
            {
                text += " " + string.Join(" ", mAppends);
            }
            if (mDistinctAppends.Any())
            {
                text += " " + string.Join(" ", mDistinctAppends.Distinct());
            }
            return text;
        }

        public TextFeatureProcessor With(TextFeature feature)
        {
            mFeatures.Add(Preconditions.CheckNotNullArgument(feature));
            return this;
        }

        public override string ToString()
        {
            var sb = new StringBuilder(GetType().Name).Append(":\n");
            foreach (TextFeature feature in mFeatures)
            {
                sb.AppendLine(feature.ToString());
            }
            return sb.ToString();
        }
    }

    public abstract class TextFeature
    {
        private readonly string mMarkToken;
        private Strings mSearchTerms;
        private bool mIsSearchModified = true;
        private Regex mRegex;

        protected TextFeature(string markToken)
        {
            mMarkToken = Preconditions.CheckNotNullArgument(markToken);
            Operation = TextFeatureOperation.Replace;
            IsEmcloseMarkTokenWithSpace = false;
        }

        public Strings SearchTerms
        {
            get { return mSearchTerms; }
            protected set
            {
                mIsSearchModified = true;
                mSearchTerms = value;
            }
        }

        public string MarkToken
        {
            get
            {
                return IsEmcloseMarkTokenWithSpace 
                    ? string.Format(" {0} ", mMarkToken) 
                    : mMarkToken;
            }
        }

        public TextFeatureOperation Operation { get; set; }
        public bool IsEmcloseMarkTokenWithSpace { get; set; }

        public Regex Regex
        {
            get
            {
                if (mIsSearchModified)
                {
                    var regexOptions = RegexOptions.None;
                    string pattern = GetPattern(ref regexOptions);
                    mRegex = new Regex(pattern, regexOptions | RegexOptions.Compiled);
                    mIsSearchModified = false;
                }
                return mRegex;
            }
        }

        protected virtual string GetPattern(ref RegexOptions options)
        {
            throw new NotImplementedException();
        }

        protected internal virtual string PerformCustomOperation(string input)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return string.Format("{0} [op: {1}; mark: {2}]", GetType().Name, Operation, MarkToken);
        }
    }

    public class CustomTextFeature : TextFeature
    {
        public CustomTextFeature(Strings searchTerms, string markToken)
            : base(markToken)
        {
            SearchTerms = searchTerms;
        }

        public Func<string> OnGetPattern { get; set; }

        protected override string GetPattern(ref RegexOptions options)
        {
            Preconditions.CheckNotNullArgument(OnGetPattern);
            return OnGetPattern();
        }
    }

    public class TermsOrTextFeature : TextFeature
    {
        public TermsOrTextFeature(Strings searchTerms, string markToken)
            : base(markToken)
        {
            SearchTerms = searchTerms;
        }

        protected TermsOrTextFeature(string markToken)
            : base(markToken)
        {
        }

        public bool IsWordBoundaryEnclosing { get; set; }

        protected override string GetPattern(ref RegexOptions options)
        {
            if (IsWordBoundaryEnclosing)
            {
                return "(" + string.Join("|", SearchTerms
                    .OrderByDescending(s => s.Length)
                    .Select(Regex.Escape)
                    .Select(s => string.Format("{0}{1}{2}", Char.IsLetter(s.First()) ? "\\b" : "", s, Char.IsLetter(s.Last()) ? "\\b" : ""))
                    .ToArray()) + ")";
            }
            return "(" + string.Join("|", SearchTerms.OrderByDescending(s => s.Length).Select(Regex.Escape).ToArray()) + ")";
        }
    }
}