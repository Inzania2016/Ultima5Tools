using System.Text;
using U5.Core.Utilities;

namespace U5.Core.Formats.Tlk
{
    public sealed class TlkDisassemblerOptions
    {
        public bool IncludeTailElements { get; set; } = true;
    }

    public sealed class TlkDisassembledField
    {
        public required string Name { get; init; }

        public required int OffsetWithinBlock { get; init; }

        public required string Text { get; init; }

        public required byte[] RawBytes { get; init; }
    }

    public sealed class TlkDisassembledKeywordGroup
    {
        public required int GroupNumber { get; init; }

        public required int OffsetWithinBlock { get; init; }

        public required IReadOnlyList<string> Keywords { get; init; }

        public required string AnswerText { get; init; }

        public required byte[] RawBytes { get; init; }
    }

    public sealed class TlkDisassembledRoute
    {
        public required IReadOnlyList<string> Tokens { get; init; }

        public required string Target { get; init; }
    }

    public sealed class TlkDisassembledResponse
    {
        public required IReadOnlyList<string> Tokens { get; init; }

        public required IReadOnlyList<string> Actions { get; init; }
    }

    public sealed class TlkDisassembledNode
    {
        public required string Label { get; init; }

        public required string Prompt { get; init; }

        public required IReadOnlyList<string> PromptActions { get; init; }

        public required IReadOnlyList<string> DefaultActions { get; init; }

        public required IReadOnlyList<TlkDisassembledResponse> Responses { get; init; }
    }

    public sealed class TlkNpcDisassembly
    {
        public required int EntryIndex { get; init; }

        public required ushort NpcId { get; init; }

        public required int BlockOffset { get; init; }

        public required int BlockSize { get; init; }

        public required IReadOnlyList<TlkDisassembledField> FixedEntries { get; init; }

        public required IReadOnlyList<TlkDisassembledKeywordGroup> KeywordGroups { get; init; }

        public required IReadOnlyList<TlkDisassembledRoute> TailRoutes { get; init; }

        public required IReadOnlyList<TlkDisassembledNode> ScriptNodes { get; init; }

        public required IReadOnlyList<TlkTailElement> TailElements { get; init; }

        public required IReadOnlyList<string> Warnings { get; init; }
    }

    public abstract class TlkTailElement
    {
        public TlkTailElement(int offsetWithinBlock, string displayText, byte[] rawBytes)
        {
            OffsetWithinBlock = offsetWithinBlock;
            DisplayText = displayText;
            RawBytes = rawBytes;
        }

        public int OffsetWithinBlock { get; }

        public string DisplayText { get; }

        public byte[] RawBytes { get; }
    }

    public sealed class TlkTailOrElement : TlkTailElement
    {
        public TlkTailOrElement(int offsetWithinBlock, byte[] rawBytes)
            : base(offsetWithinBlock, "<OR>", rawBytes)
        {
        }
    }

    public sealed class TlkTailLabelElement : TlkTailElement
    {
        public TlkTailLabelElement(int offsetWithinBlock, string label, byte[] rawBytes)
            : base(offsetWithinBlock, $"<{label}>", rawBytes)
        {
            Label = label;
        }

        public string Label { get; }
    }

    public sealed class TlkTailKeywordElement : TlkTailElement
    {
        public TlkTailKeywordElement(int offsetWithinBlock, string keyword, byte[] rawBytes)
            : base(offsetWithinBlock, keyword, rawBytes)
        {
            Keyword = keyword;
        }

        public string Keyword { get; }
    }

    public sealed class TlkTailEndSentinelElement : TlkTailElement
    {
        public TlkTailEndSentinelElement(int offsetWithinBlock, byte[] rawBytes)
            : base(offsetWithinBlock, "<END_SENTINEL>", rawBytes)
        {
        }
    }

    public sealed class TlkTailTextElement : TlkTailElement
    {
        public TlkTailTextElement(
            int offsetWithinBlock,
            string displayText,
            string visibleText,
            IReadOnlyList<TlkDecodedAction> actions,
            IReadOnlyList<TlkSegmentInstruction> instructions,
            byte[] rawBytes)
            : base(offsetWithinBlock, displayText, rawBytes)
        {
            VisibleText = visibleText;
            Actions = actions;
            Instructions = instructions;
        }

        public string VisibleText { get; }

        public IReadOnlyList<TlkDecodedAction> Actions { get; }

        public IReadOnlyList<TlkSegmentInstruction> Instructions { get; }

        public IEnumerable<string> ToHumanActions(bool includeBeginNode = false)
        {
            foreach (TlkSegmentInstruction instruction in Instructions)
            {
                if (instruction is TlkSegmentTextInstruction textInstruction)
                {
                    if (!string.IsNullOrWhiteSpace(textInstruction.Text))
                    {
                        yield return $"TEXT {QuoteForDisplay(textInstruction.Text)}";
                    }

                    continue;
                }

                if (instruction is TlkSegmentActionInstruction actionInstruction)
                {
                    if (!includeBeginNode && actionInstruction.Action.Kind == TlkDecodedActionKind.BeginNode)
                    {
                        continue;
                    }

                    yield return actionInstruction.Action.ToHumanString();
                }
            }
        }

        public IEnumerable<string> GetPromptActions()
        {
            foreach (TlkSegmentInstruction instruction in Instructions)
            {
                if (instruction is not TlkSegmentActionInstruction actionInstruction)
                {
                    continue;
                }

                if (actionInstruction.Action.Kind == TlkDecodedActionKind.BeginNode)
                {
                    continue;
                }

                yield return actionInstruction.Action.ToHumanString();
            }
        }

        private static string QuoteForDisplay(string value)
        {
            return $"\"{value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)}\"";
        }
    }

    public enum TlkDecodedActionKind
    {
        BeginNode,
        GotoLabel,
        EndDialog,
        Pause,
        JoinParty,
        TakeGold,
        GiveItem,
        AskName,
        KarmaGain,
        KarmaLoss,
        CallGuards,
        IfKnowsName,
        KeyWait,
        CheckToken,
        Fallthrough
    }

    public sealed class TlkDecodedAction
    {
        public required TlkDecodedActionKind Kind { get; init; }

        public string? Argument { get; init; }

        public string ToHumanString()
        {
            return Kind switch
            {
                TlkDecodedActionKind.BeginNode => $"BEGIN_NODE({Argument})",
                TlkDecodedActionKind.GotoLabel => $"GOTO {Argument}",
                TlkDecodedActionKind.EndDialog => "END_DIALOG",
                TlkDecodedActionKind.Pause => "PAUSE",
                TlkDecodedActionKind.JoinParty => "JOIN_PARTY",
                TlkDecodedActionKind.TakeGold => $"TAKE_GOLD({Argument})",
                TlkDecodedActionKind.GiveItem => $"GIVE_ITEM({Argument})",
                TlkDecodedActionKind.AskName => "ASK_NAME",
                TlkDecodedActionKind.KarmaGain => "KARMA_GAIN",
                TlkDecodedActionKind.KarmaLoss => "KARMA_LOSS?",
                TlkDecodedActionKind.CallGuards => "CALL_GUARDS",
                TlkDecodedActionKind.IfKnowsName => $"IF_KNOWS_NAME({Argument})",
                TlkDecodedActionKind.KeyWait => "KEY_WAIT",
                TlkDecodedActionKind.CheckToken => $"CHECK_TOKEN({Argument})",
                TlkDecodedActionKind.Fallthrough => "NO_OP",
                _ => Kind.ToString().ToUpperInvariant()
            };
        }
    }

    public abstract class TlkSegmentInstruction
    {
    }

    public sealed class TlkSegmentTextInstruction : TlkSegmentInstruction
    {
        public required string Text { get; init; }
    }

    public sealed class TlkSegmentActionInstruction : TlkSegmentInstruction
    {
        public required TlkDecodedAction Action { get; init; }
    }

    public static class TlkDisassembler
    {
        public static TlkNpcDisassembly Disassemble(TlkNpcAnalysis analysis, TlkDataOvlDictionary? dictionary = null, TlkDisassemblerOptions? options = null)
        {
            options ??= new TlkDisassemblerOptions();

            List<TlkDisassembledField> fixedEntries = analysis.FixedEntries
                .Select(entry => new TlkDisassembledField
                {
                    Name = entry.Name,
                    OffsetWithinBlock = entry.OffsetWithinBlock,
                    Text = entry.DecodedText,
                    RawBytes = entry.RawBytes
                })
                .ToList();

            List<TlkDisassembledKeywordGroup> keywordGroups = analysis.KeywordGroups
                .Select((group, index) => new TlkDisassembledKeywordGroup
                {
                    GroupNumber = index + 1,
                    OffsetWithinBlock = group.OffsetWithinBlock,
                    Keywords = group.Keywords,
                    AnswerText = group.AnswerText,
                    RawBytes = group.RawBytes
                })
                .ToList();

            List<TlkTailElement> tailElements = analysis.TailSegments
                .Select(segment => ParseTailSegment(segment, dictionary))
                .ToList();

            List<TlkDisassembledRoute> tailRoutes = BuildTailRoutes(tailElements);
            List<TlkDisassembledNode> scriptNodes = BuildScriptNodes(tailElements);
            List<string> warnings = BuildWarnings(analysis, tailElements);

            return new TlkNpcDisassembly
            {
                EntryIndex = analysis.EntryIndex,
                NpcId = analysis.NpcId,
                BlockOffset = analysis.BlockOffset,
                BlockSize = analysis.BlockSize,
                FixedEntries = fixedEntries,
                KeywordGroups = keywordGroups,
                TailRoutes = tailRoutes,
                ScriptNodes = scriptNodes,
                TailElements = tailElements,
                Warnings = warnings
            };
        }

        public static string RenderTextReport(TlkNpcDisassembly disassembly, TlkDisassemblerOptions? options = null)
        {
            options ??= new TlkDisassemblerOptions();

            StringWriter writer = new StringWriter();
            writer.WriteLine($"=== NPC Entry {disassembly.EntryIndex:D3} / NpcId {disassembly.NpcId:D3} / Block 0x{disassembly.BlockOffset:X4} / Size {disassembly.BlockSize} ===");

            if (disassembly.FixedEntries.Count > 0)
            {
                writer.WriteLine("  Fixed Entries:");
                foreach (TlkDisassembledField entry in disassembly.FixedEntries)
                {
                    writer.WriteLine($"    [{entry.OffsetWithinBlock:X4}] {entry.Name}: {entry.Text}");
                    writer.WriteLine($"      Raw: {HexFormatting.ToHex(entry.RawBytes)}");
                }
            }

            if (disassembly.KeywordGroups.Count > 0)
            {
                writer.WriteLine("  Keyword Groups:");
                foreach (TlkDisassembledKeywordGroup group in disassembly.KeywordGroups)
                {
                    writer.WriteLine($"    [{group.OffsetWithinBlock:X4}] Group {group.GroupNumber:D2}");
                    writer.WriteLine($"      Keywords: {string.Join(" | ", group.Keywords)}");
                    writer.WriteLine($"      Answer: {group.AnswerText}");
                    writer.WriteLine($"      Raw: {HexFormatting.ToHex(group.RawBytes)}");
                }
            }

            if (disassembly.TailRoutes.Count > 0)
            {
                writer.WriteLine("  Tail Routes:");
                foreach (TlkDisassembledRoute route in disassembly.TailRoutes)
                {
                    writer.WriteLine($"    [{string.Join(" | ", route.Tokens)}] -> {route.Target}");
                }
            }

            if (disassembly.ScriptNodes.Count > 0)
            {
                writer.WriteLine("  Script Nodes:");
                foreach (TlkDisassembledNode node in disassembly.ScriptNodes)
                {
                    writer.WriteLine($"    {node.Label}");
                    writer.WriteLine($"      Prompt: {node.Prompt}");

                    if (node.PromptActions.Count > 0)
                    {
                        writer.WriteLine("      Prompt Actions:");
                        foreach (string action in node.PromptActions)
                        {
                            writer.WriteLine($"        - {action}");
                        }
                    }

                    if (node.DefaultActions.Count > 0)
                    {
                        writer.WriteLine("      Default:");
                        foreach (string action in node.DefaultActions)
                        {
                            writer.WriteLine($"        - {action}");
                        }
                    }

                    foreach (TlkDisassembledResponse response in node.Responses)
                    {
                        writer.WriteLine($"      Response [{string.Join(" | ", response.Tokens)}]:");
                        foreach (string action in response.Actions)
                        {
                            writer.WriteLine($"        - {action}");
                        }
                    }
                }
            }

            if (disassembly.Warnings.Count > 0)
            {
                writer.WriteLine("  Warnings:");
                foreach (string warning in disassembly.Warnings)
                {
                    writer.WriteLine($"    - {warning}");
                }
            }

            if (options.IncludeTailElements && disassembly.TailElements.Count > 0)
            {
                writer.WriteLine("  Tail Elements:");
                foreach (TlkTailElement element in disassembly.TailElements)
                {
                    writer.WriteLine($"    [{element.OffsetWithinBlock:X4}] {element.DisplayText}");
                    writer.WriteLine($"      Raw: {HexFormatting.ToHex(element.RawBytes)}");
                }
            }

            writer.WriteLine();
            return writer.ToString();
        }

        private static TlkTailElement ParseTailSegment(TlkTailSegment segment, TlkDataOvlDictionary? dictionary)
        {
            byte[] rawBytes = segment.RawBytes;

            if (rawBytes.Length == 2 && rawBytes[0] == 0x87 && rawBytes[1] == 0x00)
            {
                return new TlkTailOrElement(segment.OffsetWithinBlock, rawBytes);
            }

            if (rawBytes.Length == 2 && rawBytes[1] == 0x00 && rawBytes[0] >= 0x91 && rawBytes[0] <= 0x9F)
            {
                return new TlkTailLabelElement(segment.OffsetWithinBlock, GetLabelName(rawBytes[0]), rawBytes);
            }

            if (rawBytes.Length >= 2 && rawBytes[0] == 0x90 && rawBytes[1] == 0x9F)
            {
                return new TlkTailEndSentinelElement(segment.OffsetWithinBlock, rawBytes);
            }

            TlkSegmentParseResult parseResult = AnalyzeSegment(rawBytes, dictionary);
            if (parseResult.IsResponseToken)
            {
                return new TlkTailKeywordElement(segment.OffsetWithinBlock, parseResult.VisibleText, rawBytes);
            }

            return new TlkTailTextElement(
                segment.OffsetWithinBlock,
                parseResult.DecodedDisplay,
                parseResult.VisibleText,
                parseResult.Actions,
                parseResult.Instructions,
                rawBytes);
        }

        private static TlkSegmentParseResult AnalyzeSegment(byte[] rawBytes, TlkDataOvlDictionary? dictionary)
        {
            TlkTextDecoder decoder = new TlkTextDecoder(dictionary);
            TlkDecodedString decoded = decoder.DecodeZeroTerminated(rawBytes, 0);

            List<TlkDecodedAction> actions = new List<TlkDecodedAction>();
            List<TlkSegmentInstruction> instructions = new List<TlkSegmentInstruction>();
            List<TlkVisibleAtom> currentTextAtoms = new List<TlkVisibleAtom>();
            List<string> visibleFragments = new List<string>();

            void FlushText()
            {
                if (currentTextAtoms.Count == 0)
                {
                    return;
                }

                string text = BuildVisibleText(currentTextAtoms);
                currentTextAtoms.Clear();
                if (text.Length == 0)
                {
                    return;
                }

                visibleFragments.Add(text);
                instructions.Add(new TlkSegmentTextInstruction { Text = text });
            }

            void AddAction(TlkDecodedAction action)
            {
                FlushText();
                actions.Add(action);
                instructions.Add(new TlkSegmentActionInstruction { Action = action });
            }

            int position = 0;
            while (position < rawBytes.Length)
            {
                byte value = rawBytes[position];
                if (value == 0x00)
                {
                    break;
                }

                if (value == 0x81)
                {
                    currentTextAtoms.Add(new TlkVisibleAtom { Text = "<AVATAR_NAME>", Kind = TlkVisibleAtomKind.ControlPlaceholder });
                    position++;
                    continue;
                }

                if (value == 0x8D)
                {
                    currentTextAtoms.Add(new TlkVisibleAtom { Text = "<NEWLINE>", Kind = TlkVisibleAtomKind.ControlPlaceholder });
                    position++;
                    continue;
                }

                if (value == 0x8E)
                {
                    currentTextAtoms.Add(new TlkVisibleAtom { Text = "<MAGIC_WORD>", Kind = TlkVisibleAtomKind.ControlPlaceholder });
                    position++;
                    continue;
                }

                if (value == 0x90 && position + 1 < rawBytes.Length && rawBytes[position + 1] >= 0x91 && rawBytes[position + 1] <= 0x9F)
                {
                    AddAction(new TlkDecodedAction { Kind = TlkDecodedActionKind.BeginNode, Argument = GetLabelName(rawBytes[position + 1]) });
                    position += 2;
                    continue;
                }

                if (value >= 0x91 && value <= 0x9F)
                {
                    AddAction(new TlkDecodedAction { Kind = TlkDecodedActionKind.GotoLabel, Argument = GetLabelName(value) });
                    position++;
                    continue;
                }

                if (value == 0x82)
                {
                    AddAction(new TlkDecodedAction { Kind = TlkDecodedActionKind.EndDialog });
                    position++;
                    continue;
                }

                if (value == 0x83)
                {
                    AddAction(new TlkDecodedAction { Kind = TlkDecodedActionKind.Pause });
                    position++;
                    continue;
                }

                if (value == 0x84)
                {
                    AddAction(new TlkDecodedAction { Kind = TlkDecodedActionKind.JoinParty });
                    position++;
                    continue;
                }

                if (value == 0x85 && position + 3 < rawBytes.Length)
                {
                    AddAction(new TlkDecodedAction { Kind = TlkDecodedActionKind.TakeGold, Argument = DecodeGoldAmount(rawBytes, position + 1) });
                    position += 4;
                    continue;
                }

                if (value == 0x86 && position + 1 < rawBytes.Length)
                {
                    AddAction(new TlkDecodedAction { Kind = TlkDecodedActionKind.GiveItem, Argument = ResolveTokenName(rawBytes[position + 1], dictionary) });
                    position += 2;
                    continue;
                }

                if (value == 0x88)
                {
                    AddAction(new TlkDecodedAction { Kind = TlkDecodedActionKind.AskName });
                    position++;
                    continue;
                }

                if (value == 0x89)
                {
                    AddAction(new TlkDecodedAction { Kind = TlkDecodedActionKind.KarmaGain });
                    position++;
                    continue;
                }

                if (value == 0x8A)
                {
                    AddAction(new TlkDecodedAction { Kind = TlkDecodedActionKind.KarmaLoss });
                    position++;
                    continue;
                }

                if (value == 0x8B)
                {
                    AddAction(new TlkDecodedAction { Kind = TlkDecodedActionKind.CallGuards });
                    position++;
                    continue;
                }

                if (value == 0x8C)
                {
                    string trueHandler = position + 1 < rawBytes.Length ? GetHandlerName(rawBytes[position + 1]) : "<MISSING>";
                    string? falseHandler = null;
                    int consumed = 2;

                    if (position + 2 < rawBytes.Length && IsConditionalHandlerByte(rawBytes[position + 2]))
                    {
                        falseHandler = GetHandlerName(rawBytes[position + 2]);
                        consumed = 3;
                    }

                    AddAction(new TlkDecodedAction
                    {
                        Kind = TlkDecodedActionKind.IfKnowsName,
                        Argument = falseHandler is null ? trueHandler : $"{trueHandler}, {falseHandler}"
                    });

                    position += Math.Min(consumed, rawBytes.Length - position);
                    continue;
                }

                if (value == 0x8F)
                {
                    AddAction(new TlkDecodedAction { Kind = TlkDecodedActionKind.KeyWait });
                    position++;
                    continue;
                }

                if (value == 0xFE && position + 1 < rawBytes.Length)
                {
                    AddAction(new TlkDecodedAction { Kind = TlkDecodedActionKind.CheckToken, Argument = ResolveTokenName(rawBytes[position + 1], dictionary) });
                    position += 2;
                    continue;
                }

                if (value == 0xFF)
                {
                    AddAction(new TlkDecodedAction { Kind = TlkDecodedActionKind.Fallthrough });
                    position++;
                    continue;
                }

                currentTextAtoms.Add(DecodeVisibleAtom(value, dictionary));
                position++;
            }

            FlushText();

            string visibleText = string.Concat(visibleFragments);
            bool isResponseToken = actions.Count == 0
                && visibleText.Length > 0
                && visibleText.Length <= 5
                && !visibleText.Contains(' ')
                && !visibleText.Contains("<NEWLINE>", StringComparison.Ordinal)
                && visibleText.All(char.IsLetterOrDigit);

            return new TlkSegmentParseResult
            {
                DecodedDisplay = decoded.DecodedText,
                VisibleText = visibleText,
                Actions = actions,
                Instructions = instructions,
                IsResponseToken = isResponseToken
            };
        }

        private static List<TlkDisassembledRoute> BuildTailRoutes(IReadOnlyList<TlkTailElement> tailElements)
        {
            List<TlkDisassembledRoute> routes = new List<TlkDisassembledRoute>();
            int index = 0;

            while (index < tailElements.Count)
            {
                if (tailElements[index] is TlkTailTextElement textElement
                    && textElement.Actions.Any(action => action.Kind == TlkDecodedActionKind.BeginNode))
                {
                    index = SkipNode(tailElements, index);
                    continue;
                }

                if (tailElements[index] is not TlkTailKeywordElement keywordElement)
                {
                    index++;
                    continue;
                }

                List<string> tokens = new List<string> { keywordElement.Keyword };
                index++;

                while (index + 1 < tailElements.Count
                    && tailElements[index] is TlkTailOrElement
                    && tailElements[index + 1] is TlkTailKeywordElement alternateKeyword)
                {
                    tokens.Add(alternateKeyword.Keyword);
                    index += 2;
                }

                if (index < tailElements.Count)
                {
                    if (tailElements[index] is TlkTailLabelElement labelElement)
                    {
                        routes.Add(new TlkDisassembledRoute { Tokens = tokens, Target = labelElement.Label });
                        index++;
                        continue;
                    }

                    if (tailElements[index] is TlkTailTextElement targetText
                        && !targetText.Actions.Any(action => action.Kind == TlkDecodedActionKind.BeginNode))
                    {
                        string target = targetText.VisibleText;
                        if (string.IsNullOrWhiteSpace(target))
                        {
                            List<string> actionStrings = targetText.ToHumanActions().ToList();
                            target = actionStrings.Count > 0 ? string.Join("; ", actionStrings) : targetText.DisplayText;
                        }

                        routes.Add(new TlkDisassembledRoute { Tokens = tokens, Target = target });
                        index++;
                        continue;
                    }
                }
            }

            return routes;
        }

        private static int SkipNode(IReadOnlyList<TlkTailElement> tailElements, int startIndex)
        {
            int index = startIndex + 1;

            if (index < tailElements.Count && tailElements[index] is not TlkTailKeywordElement)
            {
                index++;
            }

            while (index < tailElements.Count)
            {
                if (tailElements[index] is TlkTailTextElement textElement
                    && textElement.Actions.Any(action => action.Kind == TlkDecodedActionKind.BeginNode))
                {
                    break;
                }

                index++;
            }

            return index;
        }

        private static List<TlkDisassembledNode> BuildScriptNodes(IReadOnlyList<TlkTailElement> tailElements)
        {
            List<TlkDisassembledNode> nodes = new List<TlkDisassembledNode>();
            int index = 0;

            while (index < tailElements.Count)
            {
                if (tailElements[index] is not TlkTailTextElement promptElement)
                {
                    index++;
                    continue;
                }

                TlkDecodedAction? beginNodeAction = promptElement.Actions.FirstOrDefault(action => action.Kind == TlkDecodedActionKind.BeginNode);
                if (beginNodeAction is null)
                {
                    index++;
                    continue;
                }

                string label = beginNodeAction.Argument ?? "<UNKNOWN_LABEL>";
                if (label == "END_SENTINEL")
                {
                    index++;
                    continue;
                }

                List<string> promptActions = promptElement.GetPromptActions().ToList();
                List<string> defaultActions = new List<string>();
                List<TlkDisassembledResponse> responses = new List<TlkDisassembledResponse>();
                index++;

                if (index < tailElements.Count)
                {
                    if (tailElements[index] is TlkTailLabelElement defaultLabel)
                    {
                        defaultActions.Add($"GOTO {defaultLabel.Label}");
                        index++;
                    }
                    else if (tailElements[index] is TlkTailTextElement defaultText
                        && !defaultText.Actions.Any(action => action.Kind == TlkDecodedActionKind.BeginNode))
                    {
                        defaultActions.AddRange(defaultText.ToHumanActions());
                        index++;
                    }
                }

                while (index < tailElements.Count)
                {
                    if (tailElements[index] is TlkTailTextElement nextPrompt
                        && nextPrompt.Actions.Any(action => action.Kind == TlkDecodedActionKind.BeginNode))
                    {
                        break;
                    }

                    if (tailElements[index] is not TlkTailKeywordElement responseToken)
                    {
                        if (tailElements[index] is TlkTailLabelElement)
                        {
                            break;
                        }

                        index++;
                        continue;
                    }

                    List<string> tokens = new List<string> { responseToken.Keyword };
                    index++;

                    while (index + 1 < tailElements.Count
                        && tailElements[index] is TlkTailOrElement
                        && tailElements[index + 1] is TlkTailKeywordElement alternateResponseToken)
                    {
                        tokens.Add(alternateResponseToken.Keyword);
                        index += 2;
                    }

                    List<string> responseActions = new List<string>();
                    if (index < tailElements.Count)
                    {
                        if (tailElements[index] is TlkTailLabelElement responseLabel)
                        {
                            responseActions.Add($"GOTO {responseLabel.Label}");
                            index++;
                        }
                        else if (tailElements[index] is TlkTailTextElement responseText
                            && !responseText.Actions.Any(action => action.Kind == TlkDecodedActionKind.BeginNode))
                        {
                            responseActions.AddRange(responseText.ToHumanActions());
                            index++;
                        }
                    }

                    responses.Add(new TlkDisassembledResponse { Tokens = tokens, Actions = responseActions });
                }

                nodes.Add(new TlkDisassembledNode
                {
                    Label = label,
                    Prompt = string.IsNullOrWhiteSpace(promptElement.VisibleText) ? promptElement.DisplayText : promptElement.VisibleText,
                    PromptActions = promptActions,
                    DefaultActions = defaultActions,
                    Responses = responses
                });
            }

            return nodes;
        }

        private static List<string> BuildWarnings(TlkNpcAnalysis analysis, IReadOnlyList<TlkTailElement> tailElements)
        {
            List<string> warnings = new List<string>();

            foreach (TlkTailElement element in tailElements)
            {
                if (element is not TlkTailTextElement textElement)
                {
                    continue;
                }

                bool mentionsThreeGold = textElement.VisibleText.Contains("3 gold", StringComparison.OrdinalIgnoreCase);
                bool takesFourGold = textElement.Actions.Any(action => action.Kind == TlkDecodedActionKind.TakeGold && action.Argument == "004");
                if (mentionsThreeGold && takesFourGold)
                {
                    warnings.Add("Possible mismatch: text mentions 3 gold but script deducts 4.");
                }
            }

            string wholeEntryText = string.Join("\n", analysis.FixedEntries.Select(entry => entry.DecodedText))
                + "\n"
                + string.Join("\n", analysis.TailSegments.Select(segment => segment.DecodedText));

            if (wholeEntryText.Contains("3 gold", StringComparison.OrdinalIgnoreCase)
                && tailElements.OfType<TlkTailTextElement>().Any(text => text.Actions.Any(action => action.Kind == TlkDecodedActionKind.TakeGold && action.Argument == "004")))
            {
                warnings.Add("Possible dialogue/script mismatch in entry: text references 3 gold while an action deducts 4.");
            }

            return warnings.Distinct(StringComparer.Ordinal).ToList();
        }

        private static string DecodeGoldAmount(byte[] rawBytes, int offset)
        {
            StringBuilder builder = new StringBuilder(3);
            for (int i = 0; i < 3 && offset + i < rawBytes.Length; i++)
            {
                byte value = rawBytes[offset + i];
                if (value >= 0xB0 && value <= 0xB9)
                {
                    builder.Append((char)('0' + (value - 0xB0)));
                }
            }

            return builder.ToString();
        }

        private static TlkVisibleAtom DecodeVisibleAtom(byte value, TlkDataOvlDictionary? dictionary)
        {
            if (value < 0x81)
            {
                return new TlkVisibleAtom { Text = ResolveTokenName(value, dictionary), Kind = TlkVisibleAtomKind.Dictionary };
            }

            if (value == 0xA2)
            {
                return new TlkVisibleAtom { Text = "<QUOTE>", Kind = TlkVisibleAtomKind.ControlPlaceholder };
            }

            if (value >= 0xA0 && value < 0xFF)
            {
                return new TlkVisibleAtom { Text = ((char)(value - 0x80)).ToString(), Kind = TlkVisibleAtomKind.Ascii };
            }

            return new TlkVisibleAtom { Text = $"<0x{value:X2}>", Kind = TlkVisibleAtomKind.ControlPlaceholder };
        }

        private static string BuildVisibleText(IReadOnlyList<TlkVisibleAtom> atoms)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < atoms.Count; i++)
            {
                TlkVisibleAtom current = atoms[i];
                if (current.Kind == TlkVisibleAtomKind.Dictionary)
                {
                    if (NeedsLeadingSpace(builder, current.Text))
                    {
                        builder.Append(' ');
                    }

                    builder.Append(current.Text);

                    if (i + 1 < atoms.Count && NeedsTrailingSpace(current.Text, atoms[i + 1].Text))
                    {
                        builder.Append(' ');
                    }
                }
                else
                {
                    builder.Append(current.Text);
                }
            }

            return builder.ToString().Trim();
        }

        private static bool NeedsLeadingSpace(StringBuilder builder, string nextText)
        {
            if (builder.Length == 0 || string.IsNullOrEmpty(nextText))
            {
                return false;
            }

            char previous = builder[builder.Length - 1];
            return (char.IsLetterOrDigit(previous) || previous == ',' || previous == ';' || previous == ':')
                && char.IsLetterOrDigit(nextText[0]);
        }

        private static bool NeedsTrailingSpace(string currentText, string nextText)
        {
            if (string.IsNullOrEmpty(currentText) || string.IsNullOrEmpty(nextText))
            {
                return false;
            }

            return char.IsLetterOrDigit(currentText[currentText.Length - 1])
                && char.IsLetterOrDigit(nextText[0]);
        }

        private static string ResolveTokenName(byte tokenValue, TlkDataOvlDictionary? dictionary)
        {
            if (dictionary is null)
            {
                return $"<TOK_{tokenValue:X2}>";
            }

            string resolved = dictionary.Resolve(tokenValue);
            return string.IsNullOrWhiteSpace(resolved) ? $"<TOK_{tokenValue:X2}>" : resolved;
        }

        private static bool IsConditionalHandlerByte(byte value)
        {
            return value == 0x82
                || value == 0x88
                || value == 0xFF
                || (value >= 0x91 && value <= 0x9F);
        }

        private static string GetLabelName(byte value)
        {
            return value == 0x9F ? "END_SENTINEL" : $"LABEL_{value - 0x90}";
        }

        private static string GetHandlerName(byte value)
        {
            return value switch
            {
                0x82 => "END_DIALOG",
                0x88 => "ASK_NAME",
                0xFF => "NO_OP",
                >= 0x91 and <= 0x9F => GetLabelName(value),
                _ => $"0x{value:X2}"
            };
        }
    }

    public sealed class TlkSegmentParseResult
    {
        public required string DecodedDisplay { get; init; }

        public required string VisibleText { get; init; }

        public required IReadOnlyList<TlkDecodedAction> Actions { get; init; }

        public required IReadOnlyList<TlkSegmentInstruction> Instructions { get; init; }

        public required bool IsResponseToken { get; init; }
    }

    public sealed class TlkVisibleAtom
    {
        public required string Text { get; init; }

        public required TlkVisibleAtomKind Kind { get; init; }
    }

    public enum TlkVisibleAtomKind
    {
        Ascii,
        Dictionary,
        ControlPlaceholder
    }
}
