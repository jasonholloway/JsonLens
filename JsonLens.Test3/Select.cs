using System.Collections.Generic;

namespace JsonLens.Test3
{
    public static class Select
    {
        public static AllSelector All
            => new AllSelector(new SelectNode(null, Match.All));

        public static ObjectSelector Object
            => new ObjectSelector(new SelectNode(null, Match.Object));

        public static NoneSelector None
            => new NoneSelector(new SelectNode(null, Match.None));
    }


    public enum Match
    {
        None,
        All,
        Object,
        Prop
    }
    

    public class SelectNode
    {
        SelectNode _parent;
        List<SelectNode> _children = new List<SelectNode>();
        Match _strategy;

        public SelectNode(SelectNode parent, Match strategy)
        {
            _parent = parent;
            _strategy = strategy;
        }

        internal Match Strategy => _strategy;
        
        internal IEnumerable<SelectNode> Children => _children;

        internal void AddChild(SelectNode child)
            => _children.Add(child);

        internal SelectNode GetRoot()
            => _parent != null
                ? _parent.GetRoot()
                : this;

        protected S Add<S>(S child) where S : SelectNode
        {
            _children.Add(child);
            return child;
        }
    }


    public abstract class Selector
    {
        public Selector(SelectNode node)
        {
            Node = node;
        }

        protected SelectNode Node;

        protected SelectNode ChildNode(Match strategy)
        {
            var child = new SelectNode(Node, strategy);
            Node.AddChild(child);
            return child;
        }

        internal SelectNode GetSelectTree()
            => Node.GetRoot();        
    }
    
    public class NoneSelector : Selector
    {
        public NoneSelector(SelectNode parent) : base(parent)
        { }
    }
    
    public class ObjectSelector : Selector
    {
        public ObjectSelector(SelectNode parent) : base(parent)
        { }

        public PropSelector Prop(string name)
            => new PropSelector(ChildNode(Match.Prop));
    }

    public class PropSelector : Selector
    {
        public PropSelector(SelectNode parent) : base(parent)
        { }

        public ObjectSelector Object
            => new ObjectSelector(ChildNode(Match.Object));

        public AllSelector All
            => new AllSelector(ChildNode(Match.All));
    }

    public class AllSelector : Selector
    {
        public AllSelector(SelectNode parent) : base(parent)
        { }
    }

}
