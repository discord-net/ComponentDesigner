// using System;
// using System.Collections.Generic;
// using System.Diagnostics;
// using ComponentDesigner.Parser;
// using ComponentDesigner.Util;
//
// namespace ComponentDesigner.CX;
//
// public sealed class SyntaxDiff
// {
//     private ref struct DiffTable
//     {
//         public HashSet<DiffInfo> Diffs;
//         public Dictionary<ICXNode, DiffInfo> OldNodesMap;
//         public Dictionary<ICXNode, DiffInfo> NewNodesMap;
//
//         public DiffInfo Push(ICXNode? a, ICXNode? b, EqualityFlags flags)
//             => Push(new(a, b, flags));
//
//         public DiffInfo Push(DiffInfo info)
//         {
//             Diffs.Add(info);
//             if (info.OldNode is not null) OldNodesMap[info.OldNode] = info;
//             if (info.NewNode is not null) NewNodesMap[info.NewNode] = info;
//
//             return info;
//         }
//     }
//
//     private enum DiffKind
//     {
//         Unchanged,
//         Modified,
//         Added,
//         Removed
//     }
//
//     [Flags]
//     private enum EqualityFlags
//     {
//         Equal = 0,
//
//         SourceDiffers = 1 << 0,
//         TriviaDiffers = 1 << 1,
//         LocationDiffers = 1 << 2,
//
//         CompletelyDifferent = SourceDiffers | TriviaDiffers | LocationDiffers
//     }
//
//     private sealed class DiffInfo
//     {
//         public ICXNode? OldNode { get; set; }
//         public ICXNode? NewNode { get; set; }
//         public EqualityFlags Flags { get; set; }
//
//         public DiffKind Kind => (OldNode, NewNode) switch
//         {
//             (null, null) => DiffKind.Unchanged,
//             (null, not null) => DiffKind.Added,
//             (not null, null) => DiffKind.Removed,
//             (not null, not null) => Flags is EqualityFlags.Equal
//                 ? DiffKind.Unchanged
//                 : DiffKind.Modified
//         };
//
//         public bool IsAdded => Kind is DiffKind.Added;
//         public bool IsRemoved => Kind is DiffKind.Removed;
//         public bool IsEqual => Kind is DiffKind.Unchanged;
//         public bool IsModified => Kind is DiffKind.Modified;
//
//         public bool WasMovedOrEqual => WasMoved || IsEqual;
//
//         public bool WasMoved
//             => IsModified && !Flags.HasFlag(EqualityFlags.SourceDiffers);
//
//         public DiffInfo(
//             ICXNode? oldNode,
//             ICXNode? newNode,
//             EqualityFlags flags
//         )
//         {
//             OldNode = oldNode;
//             NewNode = newNode;
//             Flags = flags;
//         }
//
//         public override bool Equals(object? obj)
//             => obj is DiffInfo other &&
//                OldNode == other.OldNode &&
//                NewNode == other.NewNode &&
//                Flags == other.Flags;
//
//         public override int GetHashCode()
//             => Hash.Combine(OldNode, NewNode, Flags);
//     }
//
//
//     public static void Get(CXDocument oldDocument, CXDocument newDocument)
//     {
//     }
//
//
//     private static DiffInfo Diff(ref DiffTable table, ICXNode a, ICXNode b)
//     {
//         if (a.Slots.Count is 0 && b.Slots.Count is 0)
//         {
//             return table.Push(a, b, Compare(a, b));
//         }
//
//         if (a.Slots.Count > 0 && b.Slots.Count is 0)
//         {
//             // all nodes were removed
//             for (var i = 0; i < a.Slots.Count; i++)
//             {
//                 table.Push(a.Slots[i], null, EqualityFlags.CompletelyDifferent);
//             }
//
//             return table.Push(a, b, Compare(a, b));
//         }
//
//         if (a.Slots.Count is 0 && b.Slots.Count > 0)
//         {
//             // all nodes were added
//             for (var i = 0; i < b.Slots.Count; i++)
//             {
//                 table.Push(null, b.Slots[i], EqualityFlags.CompletelyDifferent);
//             }
//
//             return table.Push(a, b, Compare(a, b));
//         }
//
//         var mx = Math.Max(a.Slots.Count, b.Slots.Count);
//
//         DiffInfo? head = null;
//
//         /*
//          * old:
//          * <parent>
//          *   <a />
//          *   <c />
//          *   <b />
//          *   <d />
//          * </parent>
//          *
//          * new:
//          * <parent>
//          *   <a />
//          *   <b />
//          *   <c />
//          *   <d />
//          * </parent>
//          */
//
//         var flags = EqualityFlags.Equal;
//
//         for (var i = 0; i < mx; i++)
//         {
//             var left = a.Slots.Count > i ? a.Slots[i] : null;
//             var right = b.Slots.Count > i ? b.Slots[i] : null;
//
//             switch (left, right)
//             {
//                 case (not null, null):
//                     // no new node
//                     break;
//                 case (null, not null):
//                     // new node only
//                     break;
//
//                 case (not null, not null):
//                 {
//                     var diff = Diff(ref table, left, right);
//
//                     if (diff.IsEqual) continue;
//
//                     if (head is null)
//                     {
//                         head = diff;
//                         flags |= EqualityFlags.SourceDiffers;
//                         continue;
//                     }
//
//                     Debug.Assert(head.OldNode is not null && head.NewNode is not null);
//
//                     var headLeft = Diff(ref table, head.OldNode, right);
//                     var headRight = Diff(ref table, head.NewNode, left);
//
//                     if (headLeft.WasMovedOrEqual && headRight.WasMovedOrEqual)
//                     {
//                         
//                     }
//
//                     // flags |= EqualityFlags.SourceDiffers;
//                     //
//                     // if (oldHead?.OldNode is not null)
//                     // {
//                     //     var headDiff = Diff(ref table, oldHead.OldNode, right);
//                     //
//                     //     if (headDiff.WasMoved || headDiff.IsEqual)
//                     //     {
//                     //         // update oldHead and right
//                     //         oldHead.NewNode = right;
//                     //         oldHead.Flags = headDiff.Flags;
//                     //         
//                     //         table.Push(headDiff)
//                     //     }
//                     // }
//                 }
//                     break;
//             }
//
//             //var diff = Diff(left, right);
//         }
//
//         static EqualityFlags Compare(ICXNode a, ICXNode b)
//         {
//             var flags = EqualityFlags.Equal;
//
//             if (
//                 !a.LeadingTrivia.Equals(b.LeadingTrivia) ||
//                 !a.TrailingTrivia.Equals(b.TrailingTrivia)
//             )
//             {
//                 flags |= EqualityFlags.TriviaDiffers;
//             }
//
//             if (!a.TextSpan.Equals(b.TextSpan))
//                 flags |= EqualityFlags.LocationDiffers;
//
//             if (!a.Source![a.TextSpan].Equals(a.Source![b.TextSpan]))
//                 flags |= EqualityFlags.SourceDiffers;
//
//             return flags;
//         }
//     }
// }