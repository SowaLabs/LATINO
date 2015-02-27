﻿using System;
using System.Collections.Generic;
using System.Linq;
using Latino.Model;

namespace Latino.TextMining
{
    public class DeltaBowSpace<LabelT> : BowSpace
    {
        private readonly Dictionary<int, double> mWordDeltas = new Dictionary<int, double>();

        public List<SparseVector<double>> Initialize(ILabeledDataset<LabelT, string> labeledDataset)
        {
            return Initialize(labeledDataset, false);
        }

        public List<SparseVector<double>> Initialize(ILabeledDataset<LabelT, string> labeledDataset, bool largeScale)
        {
            bool normalizeVectors = NormalizeVectors;
            NormalizeVectors = false;
            List<SparseVector<double>> bowData = Initialize(labeledDataset.Select(d => d.Example), largeScale);
            NormalizeVectors = normalizeVectors;

            // count word label frequencies
            var labelWordCounts = new Dictionary<LabelT, Dictionary<int, int>>();
            for (int i = 0; i < bowData.Count; i++)
            {
                foreach (IdxDat<double> idxDat in bowData[i])
                {
                    LabelT label = labeledDataset[i].Label;
                    Dictionary<int, int> wordCounts;
                    if (!labelWordCounts.TryGetValue(label, out wordCounts))
                    {
                        labelWordCounts.Add(label, wordCounts = new Dictionary<int, int>());
                    }
                    int count;
                    if (!wordCounts.TryGetValue(idxDat.Idx, out count))
                    {
                        wordCounts.Add(idxDat.Idx, 1);
                    }
                    else
                    {
                        wordCounts[idxDat.Idx] = count + 1;
                    }
                }
            }

            // calc deltas
            int labelCount = labelWordCounts.Count;
            var counts = new List<double>();
            foreach (Word word in Words)
            {
                counts.Clear();
                foreach (KeyValuePair<LabelT, Dictionary<int, int>> kv in labelWordCounts)
                {
                    int count;
                    if (kv.Value.TryGetValue(word.mIdx, out count))
                    {
                        counts.Add(count);
                    }
                }
                if (counts.Any())
                {
                    double max = counts.Max();
                    mWordDeltas.Add(word.mIdx, Math.Abs(Math.Log(
                        max / Math.Max(counts.Sum() - max, 1) * (labelCount - 1), 2)));
                }
                else
                {
                    mWordDeltas.Add(word.mIdx, 1);
                }
            }

            // transform vectors using deltas
            var bowDataset = new List<SparseVector<double>>();
            foreach (SparseVector<double> bow in bowData)
            {
                foreach (IdxDat<double> idxDat in bow)
                {
                    IdxDat<double> idat = idxDat;
                    idat.Dat = idat.Dat * mWordDeltas[idat.Idx];
                }
                if (normalizeVectors)
                {
                    ModelUtils.TryNrmVecL2(bow);
                }
                bowDataset.Add(bow);
            }

            return bowDataset;
        }

        public override SparseVector<double> ProcessDocument(string document, IStemmer stemmer)
        {
            bool normalizeVectors = NormalizeVectors;
            NormalizeVectors = false;
            SparseVector<double> bow = base.ProcessDocument(document, stemmer);
            NormalizeVectors = normalizeVectors;

            foreach (IdxDat<double> idxDat in bow)
            {
                IdxDat<double> idat = idxDat;
                idat.Dat = idat.Dat * mWordDeltas[idat.Idx];
            }
            if (normalizeVectors)
            {
                ModelUtils.TryNrmVecL2(bow);
            }
            return bow;
        }

        public Tuple<Word, double>[] GetFreqWords(int count = 50)
        {
            return mWordDeltas
                .OrderByDescending(kv => kv.Value).Take(50)
                .Select(kv => new Tuple<Word, double>(Words.First(w => w.mIdx == kv.Key), kv.Value))
                .ToArray();
        }
    }
}