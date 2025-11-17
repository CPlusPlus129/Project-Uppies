using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DialogueModule;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "InlineDialogueSequence", menuName = "Game Flow/Dialogue Events/Inline Dialogue Sequence")]
public sealed class InlineDialogueSequenceStoryEventAsset : StoryEventAsset
{
    [SerializeField]
    [Tooltip("Optional label hint used when generating runtime dialogue labels. Leave blank to auto-generate from the asset name.")]
    private string labelHint;

    [SerializeField]
    [Tooltip("Default character layer used whenever a line does not override it.")]
    private string defaultLayerName = LayerSettings.DEFAULT_LAYER_NAME;

    [SerializeField]
    [Tooltip("Ordered list of dialogue lines that will be rendered by DialogueEngine.")]
    private List<DialogueLine> lines = new List<DialogueLine>();

    public IReadOnlyList<DialogueLine> Lines => lines;

    public override async UniTask<StoryEventResult> ExecuteAsync(GameFlowContext context, CancellationToken cancellationToken)
    {
        if (lines == null || lines.Count == 0)
        {
            return StoryEventResult.Skipped("No dialogue lines configured.");
        }

        var dialogueService = await context.GetServiceAsync<IDialogueService>();
        if (dialogueService == null)
        {
            Debug.LogError("[InlineDialogueSequence] Unable to resolve IDialogueService.");
            return StoryEventResult.Failed("Dialogue service unavailable.");
        }

        if (dialogueService is not DialogueEngine_Gaslight gaslightEngine)
        {
            Debug.LogError("[InlineDialogueSequence] Active dialogue service does not support inline sequences (expects DialogueEngine_Gaslight).");
            return StoryEventResult.Failed("Dialogue engine incompatible.");
        }

        var commands = BuildCommands();
        if (commands.Count == 0)
        {
            return StoryEventResult.Skipped("All dialogue lines were empty.");
        }

        var labelId = GenerateRuntimeLabelId();
        var labelData = new LabelData(labelId, commands);
        gaslightEngine.dataManager.RegisterRuntimeLabel(labelData);

        try
        {
            await gaslightEngine.StartDialogueAsync(labelId).AttachExternalCancellation(cancellationToken);
        }
        finally
        {
            gaslightEngine.dataManager.UnregisterRuntimeLabel(labelId);
        }

        return StoryEventResult.Completed();
    }

    private List<CommandBase> BuildCommands()
    {
        var built = new List<CommandBase>(lines.Count);
        var fallbackLayer = string.IsNullOrWhiteSpace(defaultLayerName) ? LayerSettings.DEFAULT_LAYER_NAME : defaultLayerName;

        foreach (var line in lines)
        {
            if (!line.IsRenderable)
            {
                continue;
            }

            built.Add(new InlineDialogueCommand(line, fallbackLayer));
        }

        return built;
    }

    private string GenerateRuntimeLabelId()
    {
        var prefix = string.IsNullOrWhiteSpace(labelHint) ? name : labelHint.Trim();
        return $"InlineDlg_{prefix}_{Guid.NewGuid():N}";
    }

    [Serializable]
    public struct DialogueLine
    {
        [Tooltip("Determines whether this entry uses a character from Character.csv or plays narration text.")]
        public InlineDialogueAction action;

        [CharacterId]
        [Tooltip("Character ID sourced from Character.csv.")]
        public string characterId;

        [Tooltip("Optional override for the speaker name shown in the UI.")]
        public string overrideDisplayName;

        [Tooltip("Defines the position of the character art. \"None\" displays no art.")]
        public DialogueLayerOption portraitLayout;

        [TextArea]
        [Tooltip("Dialogue or narration text. Supports the same markup as standard DialogueModule CSVs.")]
        public string content;

        public bool HasSpeaker => !string.IsNullOrWhiteSpace(characterId);
        public bool HasContent => !string.IsNullOrWhiteSpace(content);
        public string CharacterId => characterId;
        public string OverrideDisplayName => overrideDisplayName;
        public string Content => content;
        public InlineDialogueAction Action => action;
        public string GetLayerOrDefault(string fallback)
        {
            return portraitLayout switch
            {
                DialogueLayerOption.Center => "charM",
                DialogueLayerOption.Right => "charR",
                DialogueLayerOption.Left => "charL",
                _ => fallback
            };
        }

        public bool IsRenderable => action switch
        {
            InlineDialogueAction.Dialogue => HasSpeaker && HasContent,
            InlineDialogueAction.Narration => HasContent,
            _ => false
        };
    }
}

public enum InlineDialogueAction
{
    Dialogue = 0,
    Narration = 1
}

public enum DialogueLayerOption
{
    None = 0,
    Left = 1,
    Center = 2,
    Right = 3
}

public sealed class CharacterIdAttribute : PropertyAttribute
{
    public const string CharacterCsvAssetPath = "Assets/Res_Local/Global/Tables/Character.csv";
}

namespace DialogueModule
{
    class InlineDialogueCommand : CommandBase
    {
        private readonly InlineDialogueSequenceStoryEventAsset.DialogueLine line;
        private readonly string fallbackLayer;

        internal InlineDialogueCommand(InlineDialogueSequenceStoryEventAsset.DialogueLine line, string fallbackLayer)
            : base(CommandID.Character, null)
        {
            this.line = line;
            this.fallbackLayer = fallbackLayer;
        }

        internal string CharacterId => line.CharacterId;
        internal bool RequiresCharacterAssets => line.Action == InlineDialogueAction.Dialogue && line.HasSpeaker;

        public override void Execute(DialogueEngine engine)
        {
            switch (line.Action)
            {
                case InlineDialogueAction.Dialogue:
                    PlayCharacterLine(engine);
                    break;
                case InlineDialogueAction.Narration:
                    PlayNarration(engine);
                    break;
            }
        }

        private void PlayCharacterLine(DialogueEngine engine)
        {
            if (!line.HasSpeaker)
            {
                PlayNarration(engine);
                return;
            }

            if (!engine.dataManager.settingDataManager.characterSettings.DataDict
                .TryGetValue(line.CharacterId, out var characterSettingData))
            {
                Debug.LogError($"[InlineDialogueCommand] Character '{line.CharacterId}' is missing from Character.csv.");
                return;
            }

            var layerName = line.GetLayerOrDefault(fallbackLayer);
            Sprite sprite = null;
            if (!string.IsNullOrEmpty(characterSettingData.fileName) &&
                engine.assetManager.CurrentUsingAssetDict.TryGetValue(characterSettingData.fileName, out var spriteObj))
            {
                sprite = spriteObj as Sprite;
            }

            engine.adapter.characterAdapter.ShowCharacter(layerName, characterSettingData, sprite);

            var parsedText = line.HasContent ? engine.dataManager.ParseDialogueText(line.Content) : string.Empty;
            AudioClip voiceClip = null;
            if (!string.IsNullOrEmpty(characterSettingData.voiceFileName) &&
                engine.assetManager.CurrentUsingAssetDict.TryGetValue(characterSettingData.voiceFileName, out var audioObj))
            {
                voiceClip = audioObj as AudioClip;
            }

            var displayName = string.IsNullOrWhiteSpace(line.OverrideDisplayName)
                ? characterSettingData.displayName
                : line.OverrideDisplayName;

            engine.adapter.PlayText(
                displayName,
                parsedText,
                voiceClip,
                characterSettingData.voiceSpeedMultiplier <= 0f ? 1f : characterSettingData.voiceSpeedMultiplier,
                characterSettingData.hasNameCardColor ? characterSettingData.nameCardColor : (Color?)null);

            isWaiting = true;
        }

        private void PlayNarration(DialogueEngine engine)
        {
            if (!line.HasContent)
            {
                return;
            }

            var parsedText = engine.dataManager.ParseDialogueText(line.Content);
            var displayName = line.OverrideDisplayName ?? string.Empty;
            engine.adapter.PlayText(displayName, parsedText, null, 1f, null);
            isWaiting = true;
        }
    }
}
