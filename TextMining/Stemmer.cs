/*==========================================================================;
 *
 *  This file is part of LATINO. See http://www.latinolib.org
 *
 *  File:    Stemmer.cs
 *  Desc:    Snowball word stemmer (LATINO wrapper)
 *  Created: Dec-2008
 *
 *  Author:  Miha Grcar
 *
 ***************************************************************************/

using System;
using SF.Snowball.Ext;

namespace Latino.TextMining
{
    /* .-----------------------------------------------------------------------
       |
       |  Class Stemmer
       |
       '-----------------------------------------------------------------------
    */
    public class Stemmer : IStemmer, ISerializable
    {
        private Language mLanguage;
        private ISnowballStemmer mStemmer;

        public Stemmer(Language language)
        {
            mLanguage = language;
            bool success = CreateStemmer();
            Utils.ThrowException(!success ? new ArgumentNotSupportedException("language") : null);
        }

        public Stemmer(BinarySerializer reader)
        {
            Load(reader); // throws ArgumentNullException, serialization-related exceptions
        }

        private bool CreateStemmer()
        {
            switch (mLanguage)
            {
                case Language.English:
                    mStemmer = new EnglishStemmer();
                    return true;
                case Language.German:
                    mStemmer = new German2Stemmer();
                    return true;
                case Language.French:
                    mStemmer = new FrenchStemmer();
                    return true;
                case Language.Spanish:
                    mStemmer = new SpanishStemmer();
                    return true;
                case Language.Italian:
                    mStemmer = new ItalianStemmer();
                    return true;
                case Language.Portuguese:
                    mStemmer = new PortugueseStemmer();
                    return true;
                case Language.Danish:
                    mStemmer = new DanishStemmer();
                    return true;
                case Language.Dutch:
                    mStemmer = new DutchStemmer();
                    return true;
                case Language.Finnish:
                    mStemmer = new FinnishStemmer();
                    return true;
                case Language.Norwegian:
                    mStemmer = new NorwegianStemmer();
                    return true;
                case Language.Russian:
                    mStemmer = new RussianStemmer();
                    return true;
                case Language.Swedish:
                    mStemmer = new SwedishStemmer();
                    return true;
                default:
                    return false;
            }
        }

        // *** IStemmer interface implementation ***

        public string GetStem(string word)
        {
            Utils.ThrowException(word == null ? new ArgumentNullException("word") : null);
            try
            {
                mStemmer.SetCurrent(word);
                mStemmer.Stem();
                return mStemmer.GetCurrent();
            }
            catch { return word; }
        }

        // *** ISerializable interface implementation ***

        public void Save(BinarySerializer writer)
        {
            Utils.ThrowException(writer == null ? new ArgumentNullException("writer") : null);
            // the following statements throw serialization-related exceptions 
            writer.WriteInt((int)mLanguage);
        }

        public void Load(BinarySerializer reader)
        {
            Utils.ThrowException(reader == null ? new ArgumentNullException("reader") : null);
            // the following statements throw serialization-related exceptions 
            mLanguage = (Language)reader.ReadInt();
            CreateStemmer();
        }
    }
}
