using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Xml;

namespace XmlNotepad {

    public enum FindFlags {
        Normal = 0,
        Regex = 1,
        XPath = 2,
        MatchCase = 4,
        WholeWord = 8,
        Backwards = 16
    }

    public enum SearchFilter { 
        Everything, 
        Names, 
        Text, 
        Comments 
    };

    public enum FindResult {
        None,
        Found,
        NoMore,
    }

    public interface IFindTarget {

        /// <summary>
        /// Finds the next match for the given search arguments.
        /// </summary>
        /// <param name="expression">An expression representing what to find</param>
        /// <param name="flags">Flags detemine what kind of expression it is (normal, regex, xpath)
        /// and whether to search forwards or backwards and whether to match case or a whole word or not.</param>
        /// <param name="filter">What kind of nodes to filter</param>
        /// <returns>True if a match is found.</returns>
        FindResult FindNext(string expression, FindFlags flags, SearchFilter filter);

        /// <summary>
        /// Returns the screen coordinates of the current match.
        /// </summary>
        Rectangle MatchRect { get; }

        /// <summary>
        /// Replaces the current match with the given text.  You must call FindNext before calling this method.
        /// </summary>
        // <param name="replaceWith">The string to replace the matching span with</param>
        /// <returns>True if a match was replaced, or false if there is no current match right now.</returns>
        bool ReplaceCurrent(string replaceWith);

        /// <summary>
        /// Returns an XPath expression for the current selected node.
        /// </summary>
        /// <returns></returns>
        string Location { get; }

        /// <summary>
        /// Returns a namespace manager representing current location, or sets the namespace
        /// manager for the next xpath find operation.
        /// </summary>
        XmlNamespaceManager Namespaces { get; set; }

    }
}
