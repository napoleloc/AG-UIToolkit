using System;
using EncosyTower.Editor;
using EncosyTower.Editor.UIElements;
using EncosyTower.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace EncosyTower.Databases.Settings.Views
{
    using OutputFileType = DatabaseCollectionSettings.OutputFileType;
    using HelpType = HelpBoxMessageType;

    internal sealed class GoogleSheetSettingsView : SettingsView
    {
        private static readonly string[] s_jsonFilter = new[] { "JSON", "json" };

        private readonly FolderTextField _serviceAccountFileText;
        private readonly HelpBox _serviceAccountFileHelp;
        private readonly ButtonTextField _spreadsheetIdText;
        private readonly HelpBox _spreadsheetIdHelp;
        private readonly FolderTextField _outputFolderText;
        private readonly HelpBox _outputFolderHelp;
        private readonly EnumField _outputFileTypeEnum;
        private readonly Toggle _cleanOutputFolderToggle;
        private readonly Toggle _alwaysDownloadAllToggle;
        private readonly Button _downloadButton;

        private bool _serviceAccountFileValid;
        private bool _spreadsheetIdValid;
        private bool _outputFolderValid;

        private GoogleSheetContext _context;

        public GoogleSheetSettingsView(ViewResources resources)
            : base("Google Sheet", resources)
        {
            AddToClassList("google-sheet");
            AddToClassList(Constants.SETTINGS_GROUP);

            Add(new HelpBox(resources.GoogleSheet.ServiceAccount, HelpType.Info));

            _serviceAccountFileHelp = new(resources.GoogleSheet.ServiceAccountMissing, HelpType.Error);
            Add(_serviceAccountFileHelp.SetDisplay(DisplayStyle.None));

            InitPathField(
                  _serviceAccountFileText = new("Service Account File")
                , BrowseServiceAccountFile
                , PathType.File
            );

            Add(new VisualSeparator());
            Add(new HelpBox(resources.GoogleSheet.SpreadSheetId, HelpType.Info));

            _spreadsheetIdHelp = new(resources.GoogleSheet.SpreadSheetIdInvalid, HelpType.Error);
            Add(_spreadsheetIdHelp.SetDisplay(DisplayStyle.None));

            InitSpreadSheetIdField(
                  _spreadsheetIdText = new("Spread Sheet Id")
                , OpenSpreadSheetUrl
            );

            Add(new VisualSeparator());
            Add((_outputFolderHelp = new()).SetDisplay(DisplayStyle.None));

            InitPathField(
                  _outputFolderText = new("Output Folder")
                , BrowseFolder
                , PathType.Folder
            );

            Add(new VisualSeparator());

            _outputFileTypeEnum = new("Output File Type", default(OutputFileType));
            Add(_outputFileTypeEnum.AddToAlignFieldClass());

            _cleanOutputFolderToggle = new("Clean Output Folder?");
            Add(_cleanOutputFolderToggle.AddToAlignFieldClass());

            _alwaysDownloadAllToggle = new("Always Download All?");
            Add(_alwaysDownloadAllToggle.AddToAlignFieldClass());

            Add(new VisualSeparator());

            Add(_downloadButton = new(DownloadButton_OnClicked) {
#if UNITY_6000_0_OR_NEWER
                enabledSelf = false,
#endif
            });

            _downloadButton.AddToClassList("convert-button");
            _downloadButton.AddToClassList("function-button");

#if !UNITY_6000_0_OR_NEWER
            _downloadButton.SetEnabled(false);
#endif

            Add(new VisualSeparator());

            var cleanOutputFolderButton = new Button(CleanOutputFolderButton_OnClicked) {
                text = "Clean Output Folder",
            };

            cleanOutputFolderButton.AddToClassList("function-button");
            Add(cleanOutputFolderButton);

            RegisterValueChangedCallbacks();
        }

        public void Bind(GoogleSheetContext context)
        {
            _context = context;

            BindFoldout(context.GetEnabledProperty());

            {
                var prop = context.GetServiceAccountRelativeFilePathProperty();
                _serviceAccountFileText.Bind(prop);
                TryDisplayServiceAccountFileHelp(prop.stringValue);
            }

            {
                var prop = context.GetSpreadsheetIdProperty();
                _spreadsheetIdText.Bind(prop);
                TryDisplaySpreadSheetIdHelp(prop.stringValue);
            }

            {
                var prop = context.GetOutputRelativeFolderPathProperty();
                _outputFolderText.Bind(prop);
                TryDisplayOutputFolderHelp(prop.stringValue);
            }

            _cleanOutputFolderToggle.BindProperty(context.GetCleanOutputFolderProperty());
            _alwaysDownloadAllToggle.BindProperty(context.GetAlwaysDownloadAllProperty());

            {
                var prop = context.GetOutputFileTypeProperty();
                _outputFileTypeEnum.BindProperty(prop);
                InitDownloadButton((OutputFileType)prop.enumValueIndex);
            }
        }

        public override void Unbind()
        {
            _context = default;

            base.Unbind();

            _serviceAccountFileText.Unbind();
            _spreadsheetIdText.Unbind();
            _outputFolderText.Unbind();
            _outputFileTypeEnum.Unbind();
            _cleanOutputFolderToggle.Unbind();
            _alwaysDownloadAllToggle.Unbind();
        }

        protected override void OnEnabled(bool value)
        {
            RefreshDownloadButton();
        }

        private void InitSpreadSheetIdField(
              ButtonTextField element
            , Action<ButtonTextField> onClicked
        )
        {
            Add(element.AddToAlignFieldClass());

            var icon = EditorAPI.GetIcon("d_buildsettings.web.small", "buildsettings.web.small");
            var iconImage = Background.FromTexture2D(icon.image as Texture2D);

#if UNITY_6000_0_OR_NEWER
            element.Button.iconImage = iconImage;
            element.Button.text = "Open";
#else
            element.Button.SetToImageElement(iconImage);
            element.Button.SetToTextElement("Open");
#endif

            element.Clicked += onClicked;
        }

        private void RegisterValueChangedCallbacks()
        {
            _serviceAccountFileText.TextField.RegisterValueChangedCallback(OnValueChanged_EquatableTyped);
            _spreadsheetIdText.TextField.RegisterValueChangedCallback(OnValueChanged_EquatableTyped);
            _outputFolderText.TextField.RegisterValueChangedCallback(OnValueChanged_EquatableTyped);
            _outputFileTypeEnum.RegisterValueChangedCallback(OnValueChanged_ComparableUntyped);
            _cleanOutputFolderToggle.RegisterValueChangedCallback(OnValueChanged_EquatableTyped);
            _alwaysDownloadAllToggle.RegisterValueChangedCallback(OnValueChanged_EquatableTyped);

            _serviceAccountFileText.TextField.RegisterValueChangedCallback(ServiceAccountFile_OnChanged);
            _spreadsheetIdText.TextField.RegisterValueChangedCallback(SpreadSheetId_OnChanged);
            _outputFolderText.TextField.RegisterValueChangedCallback(OutputFolder_OnChanged);
            _outputFileTypeEnum.RegisterValueChangedCallback(OutputFileType_OnChanged);
        }

        private void InitDownloadButton(OutputFileType fileType)
            => _downloadButton.text = $"Download to {ObjectNames.NicifyVariableName(fileType.ToString())}";

        private void BrowseServiceAccountFile(ButtonTextField sender)
            => DirectoryAPI.OpenFilePanel(sender.TextField, "Select Service Account File", s_jsonFilter);

        private void OpenSpreadSheetUrl(ButtonTextField sender)
            => Application.OpenURL($"https://docs.google.com/spreadsheets/d/{sender.TextField.value}");

        private void ServiceAccountFile_OnChanged(ChangeEvent<string> evt)
        {
            if (evt.newValue.Contains('<'))
            {
                /// README
                Notes.ToPreventIllegalCharsExceptionWhenSearch();
                return;
            }

            TryDisplayServiceAccountFileHelp(evt.newValue);
        }

        private void SpreadSheetId_OnChanged(ChangeEvent<string> evt)
        {
            if (evt.newValue.Contains('<'))
            {
                /// README
                Notes.ToPreventIllegalCharsExceptionWhenSearch();
                return;
            }

            TryDisplaySpreadSheetIdHelp(evt.newValue);
        }

        private void OutputFolder_OnChanged(ChangeEvent<string> evt)
        {
            if (evt.newValue.Contains('<'))
            {
                /// README
                Notes.ToPreventIllegalCharsExceptionWhenSearch();
                return;
            }

            TryDisplayOutputFolderHelp(evt.newValue);
        }

        private void OutputFileType_OnChanged(ChangeEvent<Enum> evt)
        {
            if (evt.newValue is OutputFileType fileType)
            {
                InitDownloadButton(fileType);
            }
        }

        private void TryDisplayServiceAccountFileHelp(string relativePath)
        {
            _serviceAccountFileValid = DisplayIfFileNotExist(_serviceAccountFileHelp, relativePath);
            RefreshDownloadButton();
        }

        private void TryDisplaySpreadSheetIdHelp(string value)
        {
            _spreadsheetIdValid = DisplayIfStringEmpty(_spreadsheetIdHelp, value);
            RefreshDownloadButton();
        }

        private void TryDisplayOutputFolderHelp(string relativePath)
        {
            var resources = Resources.GoogleSheet;

            TryDisplayFolderHelp(
                  _outputFolderHelp
                , relativePath
                , new(resources.OutputFolderInvalid, HelpType.Error, false)
                , new(resources.OutputFolderMissing, HelpType.Warning, true)
                , ref _outputFolderValid
            );

            RefreshDownloadButton();
        }

        private void RefreshDownloadButton()
        {
            var value = Enabled
                && _serviceAccountFileValid
                && _spreadsheetIdValid
                && _outputFolderValid;

#if UNITY_6000_0_OR_NEWER
            _downloadButton.enabledSelf = value;
#else
            _downloadButton.SetEnabled(value);
#endif
        }

        private void DownloadButton_OnClicked()
        {
            var owner = _context.Property.serializedObject.targetObject;
            _context.DatabaseSettings?.Convert(DataSourceFlags.GoogleSheet, owner);
        }

        private void CleanOutputFolderButton_OnClicked()
        {
            _context.DatabaseSettings?.CleanOutputFolder(DataSourceFlags.GoogleSheet);
            AssetDatabase.Refresh();
        }
    }
}
