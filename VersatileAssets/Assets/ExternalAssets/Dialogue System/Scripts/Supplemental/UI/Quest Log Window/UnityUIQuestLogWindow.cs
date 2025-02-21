#if !(UNITY_4_3 || UNITY_4_5)
using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem {

	/// <summary>
	/// This is an implementation of the abstract QuestLogWindow class for the Unity UI.
	/// </summary>
	[AddComponentMenu("Dialogue System/UI/Unity UI/Quest/Unity UI Quest Log Window")]
	public class UnityUIQuestLogWindow : QuestLogWindow {

		/// <summary>
		/// The main quest log window panel.
		/// </summary>
		public UnityEngine.UI.Graphic mainPanel;

		/// <summary>
		/// The active quests button.
		/// </summary>
		public UnityEngine.UI.Button activeQuestsButton;

		/// <summary>
		/// The completed quests button.
		/// </summary>
		public UnityEngine.UI.Button completedQuestsButton;

		/// <summary>
		/// The quest table.
		/// </summary>
		public UnityEngine.UI.Graphic questTable;

		public UnityUIQuestGroupTemplate questGroupTemplate;

		/// <summary>
		/// The quest template.
		/// </summary>
		public UnityUIQuestTemplate questTemplate;

		/// <summary>
		/// The confirmation popup to use if the player clicks the abandon quest button.
		/// It should send ClickConfirmAbandonQuest if the player confirms, or
		/// ClickCancelAbandonQuest if the player cancels.
		/// </summary>
		public UnityEngine.UI.Graphic abandonPopup;

		/// <summary>
		/// The quest title label to set in the abandon quest dialog popup.
		/// </summary>
		public UnityEngine.UI.Text abandonQuestTitle;

		/// <summary>
		/// Set <c>true</c> to always keep a control focused; useful for gamepads.
		/// </summary>
		public bool autoFocus = false;

		public UnityEvent onContentChanged = new UnityEvent();
		
		[Serializable]
		public class AnimationTransitions {
			public string showTrigger = "Show";
			public string hideTrigger = "Hide";
		}
		
		public AnimationTransitions animationTransitions = new AnimationTransitions();
		
		/// <summary>
		/// This handler is called if the player confirms abandonment of a quest.
		/// </summary>
		private Action confirmAbandonQuestHandler = null;

		private UIShowHideController showHideController = null;

        private List<string> collapsedGroups = new List<string>();

		public override void Awake() {
			showHideController = new UIShowHideController(this.gameObject, mainPanel);
		}

		/// <summary>
		/// Hide the main panel and all of the templates on start.
		/// </summary>
		public virtual void Start() {
			UITools.RequireEventSystem();
			Tools.SetGameObjectActive(mainPanel, false);
			Tools.SetGameObjectActive(abandonPopup, false);
			Tools.SetGameObjectActive(questGroupTemplate, false);
			Tools.SetGameObjectActive(questTemplate, false);
			SetStateButtonListeners();
			SetStateToggleButtons();
			if (DialogueDebug.LogWarnings) {
				if (mainPanel == null) Debug.LogWarning(string.Format("{0}: {1} Main Panel is unassigned", new object[] { DialogueDebug.Prefix, name }));
				if (questTable == null) Debug.LogWarning(string.Format("{0}: {1} Quest Table is unassigned", new object[] { DialogueDebug.Prefix, name }));
				if (useGroups && ((questTemplate == null || !questTemplate.ArePropertiesAssigned))) {
					Debug.LogWarning(string.Format("{0}: {1} Quest Group Template or one of its properties is unassigned", new object[] { DialogueDebug.Prefix, name }));
				}
				if (questTemplate == null || !questTemplate.ArePropertiesAssigned) {
					Debug.LogWarning(string.Format("{0}: {1} Quest Template or one of its properties is unassigned", new object[] { DialogueDebug.Prefix, name }));
				}
			}
		}

		public void Update() {
			if (autoFocus && (UnityEngine.EventSystems.EventSystem.current != null)) {
				if (UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject == null) {
					GameObject go = completedQuestsButton.gameObject.activeSelf ? completedQuestsButton.gameObject : activeQuestsButton.gameObject;
					UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(go, null);
				}
			}
		}
		
		/// <summary>
		/// Open the window by showing the main panel. The bark UI may conflict with the quest
		/// log window, so temporarily disable it.
		/// </summary>
		/// <param name="openedWindowHandler">Opened window handler.</param>
		public override void OpenWindow(Action openedWindowHandler) {
			showHideController.Show(animationTransitions.showTrigger, pauseWhileOpen, openedWindowHandler, false);
			IsOpen = true;
		}

		/// <summary>
		/// Close the window by hiding the main panel. Re-enable the bark UI.
		/// </summary>
		/// <param name="closedWindowHandler">Closed window handler.</param>
		public override void CloseWindow(Action closedWindowHandler) {
			showHideController.Hide(animationTransitions.hideTrigger, closedWindowHandler);
			IsOpen = false;
		}

		/// <summary>
		/// Whenever the quest list is updated, repopulate the scroll panel.
		/// </summary>
		public override void OnQuestListUpdated() {
			if (questTable == null) return;
			ClearQuestTable();
			if (Quests.Length == 0) {
				AddQuestToTable(new QuestInfo(string.Empty, new FormattedText(NoQuestsMessage), FormattedText.empty, new FormattedText[0], new QuestState[0], false, false, false), 0);
			} else {
				AddQuestsToTable();
			}
			SetStateToggleButtons();
		}

		private void SetStateButtonListeners() {
			if (activeQuestsButton != null) {
				activeQuestsButton.onClick.RemoveListener(() => ClickShowActiveQuestsButton());
				activeQuestsButton.onClick.AddListener(() => ClickShowActiveQuestsButton());
			}
			if (completedQuestsButton != null) {
				completedQuestsButton.onClick.RemoveListener(() => ClickShowCompletedQuestsButton());
				completedQuestsButton.onClick.AddListener(() => ClickShowCompletedQuestsButton());
			}
		}

		private void SetStateToggleButtons() {
			if (activeQuestsButton != null) activeQuestsButton.interactable = !IsShowingActiveQuests;
			if (completedQuestsButton != null) completedQuestsButton.interactable = IsShowingActiveQuests;
		}

		private void ClearQuestTable() {
			if (questTable == null) return;
			foreach (Transform child in questTable.transform) {
				if (child.gameObject.activeSelf) {
					Destroy(child.gameObject);
				}
			}
			NotifyContentChanged();
		}

		private void AddQuestsToTable() {
			if (questTable == null) return;
			string currentGroup = null;
            var isCurrentGroupCollapsed = false;
			for (int i = 0; i < Quests.Length; i++) {
				if (!string.Equals(Quests[i].Group, currentGroup)) {
					currentGroup = Quests[i].Group;
					AddQuestGroupToTable(currentGroup);
                    isCurrentGroupCollapsed = collapsedGroups.Contains(currentGroup);
				}
                if (!isCurrentGroupCollapsed) {
                    AddQuestToTable(Quests[i], i);
                }
			}
			NotifyContentChanged();
		}

		/// <summary>
		/// Adds a quest group to the table using the template.
		/// </summary>
		/// <param name="questInfo">Quest info.</param>
		private void AddQuestGroupToTable(string group) {
			if (string.IsNullOrEmpty(group) || questGroupTemplate == null || !questGroupTemplate.ArePropertiesAssigned) return;
			GameObject questGroupGameObject = Instantiate(questGroupTemplate.gameObject) as GameObject;
			if (questGroupGameObject == null) {
				Debug.LogError(string.Format("{0}: {1} couldn't instantiate quest group template", new object[] { DialogueDebug.Prefix, name }));
				return;
			}
			questGroupGameObject.name = group;
			questGroupGameObject.transform.SetParent(questTable.transform, false);
			questGroupGameObject.SetActive(true);
			UnityUIQuestGroupTemplate template = questGroupGameObject.GetComponent<UnityUIQuestGroupTemplate>();
			if (template == null) return;
			template.Initialize();
			template.heading.text = group;

            // Set up the collapse/expand button:
            var button = questGroupGameObject.GetComponentInChildren<UnityEngine.UI.Button>();
            if (button != null) {
                button.onClick.AddListener(() => ClickQuestGroupFoldout(group));
            }
        }

		/// <summary>
		/// Adds a quest to the table using the template.
		/// </summary>
		/// <param name="questInfo">Quest info.</param>
		private void AddQuestToTable(QuestInfo questInfo, int questIndex) {
			if ((questTable == null) || (questTemplate == null) || !questTemplate.ArePropertiesAssigned) return;
			
			// Instantiate a copy of the template:
			GameObject questGameObject = Instantiate(questTemplate.gameObject) as GameObject;
			if (questGameObject == null) {
				Debug.LogError(string.Format("{0}: {1} couldn't instantiate quest template", new object[] { DialogueDebug.Prefix, name }));
				return;
			}
			questGameObject.name = questInfo.Heading.text;
			questGameObject.transform.SetParent(questTable.transform, false);
			questGameObject.SetActive(true);
			UnityUIQuestTemplate questControl = questGameObject.GetComponent<UnityUIQuestTemplate>();
			if (questControl == null) return;
			questControl.Initialize();
			
			// Set the heading button:
			var questHeadingButton = questControl.heading;
			var heading = questHeadingButton.GetComponentInChildren<UnityEngine.UI.Text>();
			if (heading != null) heading.text = questInfo.Heading.text;
			questHeadingButton.onClick.AddListener(() => ClickQuestFoldout(questInfo.Title));
			
			// Set details if this is the selected quest:
			if (IsSelectedQuest(questInfo)) {
				
				// Set the description:
				if (questHeadingSource == QuestHeadingSource.Name) {
					questControl.description.text = questInfo.Description.text;
					Tools.SetGameObjectActive(questControl.description, true);
				}
				
				// Set the entry description:
				if (questInfo.EntryStates.Length > 0) {
					for (int i = 0; i < questInfo.Entries.Length; i++) {
						questControl.AddEntryDescription(questInfo.Entries[i].text, questInfo.EntryStates[i]);
					}
				}
				
				UnityUIQuestTitle unityUIQuestTitle = null;
				
				// Set the track button:
				if (questControl.trackButton != null) {
					unityUIQuestTitle = questControl.trackButton.gameObject.AddComponent<UnityUIQuestTitle>();
					unityUIQuestTitle.questTitle = questInfo.Title;
					questControl.trackButton.onClick.RemoveListener(() => ClickTrackQuestButton());
					questControl.trackButton.onClick.AddListener(() => ClickTrackQuestButton());
					Tools.SetGameObjectActive(questControl.trackButton, questInfo.Trackable);
				}
				
				// Set the abandon button:
				if (questControl.abandonButton != null) {
					unityUIQuestTitle = questControl.abandonButton.gameObject.AddComponent<UnityUIQuestTitle>();
					unityUIQuestTitle.questTitle = questInfo.Title;
					questControl.abandonButton.onClick.RemoveListener(() => ClickAbandonQuestButton());
					questControl.abandonButton.onClick.AddListener(() => ClickAbandonQuestButton());
					Tools.SetGameObjectActive(questControl.abandonButton, questInfo.Abandonable);
				}
				
			} else {
				Tools.SetGameObjectActive(questControl.description, false);
				Tools.SetGameObjectActive(questControl.entryDescription, false);
				Tools.SetGameObjectActive(questControl.trackButton, false);
				Tools.SetGameObjectActive(questControl.abandonButton, false);
			}
		}
		
		public void NotifyContentChanged() {
			onContentChanged.Invoke();
		}
		
		public void ClickQuestFoldout(string questTitle) {
			ClickQuest(questTitle);
		}

        public void ClickQuestGroupFoldout(string group) {
            if (collapsedGroups.Contains(group)) {
                collapsedGroups.Remove(group);
            } else {
                collapsedGroups.Add(group);
            }
            OnQuestListUpdated();
        }

		/// <summary>
		/// Track button clicked event that sets SelectedQuest first.
		/// </summary>
		/// <param name="button">Button.</param>
		private void OnTrackButtonClicked(GameObject button) {
			SelectedQuest = button.GetComponent<UnityUIQuestTitle>().questTitle;
			ClickTrackQuest(SelectedQuest);
		}

		/// <summary>
		/// Abandon button clicked event that sets SelectedQuest first.
		/// </summary>
		/// <param name="button">Button.</param>
		private void OnAbandonButtonClicked(GameObject button) {
			SelectedQuest = button.GetComponent<UnityUIQuestTitle>().questTitle;
			ClickAbandonQuest(SelectedQuest);
		}

		/// <summary>
		/// Opens the abandon confirmation popup.
		/// </summary>
		/// <param name="title">Quest title.</param>
		/// <param name="confirmAbandonQuestHandler">Confirm abandon quest handler.</param>
		public override void ConfirmAbandonQuest(string title, Action confirmAbandonQuestHandler) {
			this.confirmAbandonQuestHandler = confirmAbandonQuestHandler;
			OpenAbandonPopup(title);
		}

		/// <summary>
		/// Opens the abandon popup modally if assigned; otherwise immediately confirms.
		/// </summary>
		/// <param name="title">Quest title.</param>
		private void OpenAbandonPopup(string title) {
			if (abandonPopup != null) {
				Tools.SetGameObjectActive(abandonPopup, true);
				if (abandonQuestTitle != null) abandonQuestTitle.text = title;
			} else {
				this.confirmAbandonQuestHandler();
			}
		}

		/// <summary>
		/// Closes the abandon popup.
		/// </summary>
		private void CloseAbandonPopup() {
			Tools.SetGameObjectActive(abandonPopup, false);
		}

		public void ClickConfirmAbandonQuestButton() {
			CloseAbandonPopup();
			confirmAbandonQuestHandler();
		}

		public void ClickCancelAbandonQuestButton() {
			CloseAbandonPopup();
		}

	}

}
#endif