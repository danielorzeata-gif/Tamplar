using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RhinoWood.Core.Domain;

namespace RhinoWood.Core.Parametric
{
    public enum OverrideMode { Add, Replace }

    /// <summary>Manual adjustment stored separately from the parametric rule (never destroys the rule).</summary>
    public sealed class NumericOverride
    {
        public OverrideMode Mode { get; set; }
        public double Value { get; set; }
        public double Apply(double calculated) => Mode == OverrideMode.Add ? calculated + Value : Value;
    }

    public sealed class OverrideInfo
    {
        public string NodeId { get; set; }
        public double Calculated { get; set; }
        public NumericOverride Override { get; set; }
        public double Final { get; set; }
    }

    public interface IGraphReader { T Get<T>(string id); }

    /// <summary>
    /// Pull-based dependency graph with dirty propagation and early cut-off: a node is re-evaluated only
    /// when one of its declared dependencies changed its fingerprint.
    /// </summary>
    public sealed class DependencyGraph
    {
        private sealed class Node
        {
            public string Id;
            public bool IsInput;
            public string[] Deps = new string[0];
            public Func<IGraphReader, object> Compute;
            public object Raw;
            public object Value;
            public string Fp;
            public int Version;
            public int EvalCount;
            public bool Dirty = true;
            public bool HasValue;
            public int[] DepVersions;
            public NumericOverride Override;
            public readonly List<string> Dependents = new List<string>();
        }

        private readonly Dictionary<string, Node> _nodes = new Dictionary<string, Node>();
        private readonly List<string> _evalLog = new List<string>();

        public IReadOnlyCollection<string> NodeIds => _nodes.Keys;
        /// <summary>Nodes actually re-evaluated since the last <see cref="ClearLog"/>.</summary>
        public IReadOnlyList<string> EvaluationLog => _evalLog;
        public void ClearLog() => _evalLog.Clear();
        public int EvaluationCount(string id) => _nodes[id].EvalCount;
        public bool Contains(string id) => _nodes.ContainsKey(id);

        public void AddInput(string id, object value)
        {
            if (_nodes.ContainsKey(id)) throw new InvalidOperationException("Duplicate node " + id);
            _nodes[id] = new Node { Id = id, IsInput = true, Raw = value };
        }

        public void AddComputed(string id, string[] deps, Func<IGraphReader, object> compute)
        {
            if (_nodes.ContainsKey(id)) throw new InvalidOperationException("Duplicate node " + id);
            foreach (var d in deps)
                if (!_nodes.ContainsKey(d)) throw new InvalidOperationException("Node " + id + " depends on unknown node " + d + " (dependencies must be defined first; graph is acyclic by construction)");
            var n = new Node { Id = id, Deps = deps, Compute = compute };
            _nodes[id] = n;
            foreach (var d in deps) _nodes[d].Dependents.Add(id);
        }

        public void Set(string id, object value)
        {
            var n = Require(id);
            if (!n.IsInput) throw new InvalidOperationException(id + " is a computed node; use SetOverride");
            if (Equals(Fingerprint(n.Raw), Fingerprint(value))) return;
            n.Raw = value;
            MarkDirty(n);
        }

        public void SetOverride(string id, NumericOverride ov)
        {
            var n = Require(id);
            n.Override = ov;
            MarkDirty(n);
        }

        public void ClearOverride(string id) => SetOverride(id, null);

        public IEnumerable<KeyValuePair<string, NumericOverride>> Overrides =>
            _nodes.Values.Where(n => n.Override != null).Select(n => new KeyValuePair<string, NumericOverride>(n.Id, n.Override));

        public OverrideInfo GetOverrideInfo(string id)
        {
            var n = Require(id);
            Ensure(n);
            double calc = Convert.ToDouble(n.IsInput ? n.Raw : n.Raw, CultureInfo.InvariantCulture);
            return new OverrideInfo { NodeId = id, Calculated = calc, Override = n.Override, Final = Convert.ToDouble(n.Value, CultureInfo.InvariantCulture) };
        }

        public T Get<T>(string id)
        {
            var n = Require(id);
            Ensure(n);
            return (T)n.Value;
        }

        public string FingerprintOf(string id) { var n = Require(id); Ensure(n); return n.Fp; }
        public int VersionOf(string id) { var n = Require(id); Ensure(n); return n.Version; }

        private Node Require(string id)
        {
            if (!_nodes.TryGetValue(id, out var n)) throw new KeyNotFoundException("Unknown graph node " + id);
            return n;
        }

        private void MarkDirty(Node n)
        {
            n.Dirty = true;
            foreach (var d in n.Dependents)
            {
                var dn = _nodes[d];
                if (!dn.Dirty) MarkDirty(dn);
            }
        }

        private sealed class Reader : IGraphReader
        {
            private readonly DependencyGraph _g; private readonly Node _owner;
            public Reader(DependencyGraph g, Node owner) { _g = g; _owner = owner; }
            public T Get<T>(string id)
            {
                if (Array.IndexOf(_owner.Deps, id) < 0)
                    throw new InvalidOperationException("Node " + _owner.Id + " read undeclared dependency " + id);
                return (T)_g.Require(id).Value;
            }
        }

        private void Ensure(Node n)
        {
            if (!n.Dirty && n.HasValue) return;
            foreach (var d in n.Deps) Ensure(_nodes[d]);

            if (!n.IsInput)
            {
                bool need = !n.HasValue;
                if (!need)
                    for (int i = 0; i < n.Deps.Length; i++)
                        if (_nodes[n.Deps[i]].Version != n.DepVersions[i]) { need = true; break; }
                if (need)
                {
                    n.Raw = n.Compute(new Reader(this, n));
                    n.EvalCount++;
                    _evalLog.Add(n.Id);
                    n.DepVersions = n.Deps.Select(d => _nodes[d].Version).ToArray();
                }
            }
            object v = n.Raw;
            if (n.Override != null && v is IConvertible)
                v = n.Override.Apply(Convert.ToDouble(v, CultureInfo.InvariantCulture));
            Commit(n, v);
            n.Dirty = false;
        }

        private void Commit(Node n, object value)
        {
            var fp = Fingerprint(value);
            if (!n.HasValue || fp != n.Fp) n.Version++;
            n.Value = value;
            n.Fp = fp;
            n.HasValue = true;
        }

        public static string Fingerprint(object v)
        {
            switch (v)
            {
                case null: return "null";
                case IFingerprint f: return f.Fingerprint;
                case double d: return d.ToString("R", CultureInfo.InvariantCulture);
                case string s: return s;
                case IFormattable fm: return fm.ToString(null, CultureInfo.InvariantCulture);
                case IEnumerable e: return "[" + string.Join(",", e.Cast<object>().Select(Fingerprint)) + "]";
                default: return v.ToString();
            }
        }
    }
}
