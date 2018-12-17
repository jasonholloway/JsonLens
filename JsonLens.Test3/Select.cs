using System.Collections.Generic;

namespace JsonLens.Test3
{
    public static class Select
    {
        public static AllSelector Any
            => new AllSelector(new SelectNode(null, Match.Any));

        public static NoneSelector None
            => new NoneSelector(new SelectNode(null, Match.None));


        public static ObjectSelector Object
            => new ObjectSelector(new SelectNode(null, Match.Object));
    }


    public enum Match
    {
        Any,
        None,

        Array,
        Object,     //the object matcher itself should include the prop map - as there's nowhere else to go in such circumstances
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

        internal Match Match => _strategy;
        
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
            => new AllSelector(ChildNode(Match.Any));

        public NoneSelector None
            => new NoneSelector(ChildNode(Match.None));
    }

    public class AllSelector : Selector
    {
        public AllSelector(SelectNode parent) : base(parent)
        { }
    }

}
