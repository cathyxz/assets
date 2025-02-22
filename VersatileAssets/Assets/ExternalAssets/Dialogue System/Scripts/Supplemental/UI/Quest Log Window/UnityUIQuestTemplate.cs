#if !(UNITY_4_3 || UNITY_4_5)
using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem {

	/// <summary>
	/// This component hooks up the elements of a Unity UI quest template.
	/// Add it to your quest template and assign the properties.
	/// </summary>
	[AddComponentMenu("Dialogue System/UI/Unity UI/Quest/Unity UI Quest Template")]
	public class UnityUIQuestTemplate : MonoBehaviour	{

		[Header("Quest Heading")]
		[Tooltip("The heading - name or description depends on window setting")]
		public UnityEngine.UI.Button heading;

		[Tooltip("Used for Description")]
		public UnityEngine.UI.Text description;

		public UnityUIQuestTemplateAlternateDescriptions alternateDescriptions = new UnityUIQuestTemplateAlternateDescriptions();
		
		[Header("Quest Entries")]
		[Tooltip("(Optional) If set, holds instantiated quest entries")]
		public Transform entryContainer;
		
		[Tooltip("Used for quest entries")]
		public UnityEngine.UI.Text entryDescription;

		public UnityUIQuestTemplateAlternateDescriptions alternateEntryDescriptions = new UnityUIQuestTemplateAlternateDescriptions();

		[Header("Buttons")]
		[Tooltip("Used for Track button if quest is trackable")]
		public UnityEngine.UI.Button trackButton;

		[Tooltip("Used for Abandon button if quest is abandonable")]
		public UnityEngine.UI.Button abandonButton;

		public bool ArePropertiesAssigned {
			get {
				return (heading != null) &&
					(description != null) && (entryDescription != null) &&
					(trackButton != null) && (abandonButton != null);
			}
		}

		private int numEntries = 0;

		public void Initialize() {
			if (description != null) description.gameObject.SetActive(false);
			if (entryDescription != null) entryDescription.gameObject.SetActive(false);
			alternateEntryDescriptions.SetActive(false);
			if (entryContainer != null) entryContainer.gameObject.SetActive(false);
		}
		
		public void AddEntryDescription(string text, QuestState entryState) {
			if (entryContainer == null) {
				
				// No container, so make entryDescription a big multi-line string:
				alternateEntryDescriptions.SetActive(false);
				if (entryDescription != null) {
					if (numEntries == 0) {
						entryDescription.gameObject.SetActive(true);
						entryDescription.text = text;
					} else {
						entryDescription.text += "\n" + text;
					}
				}
			} else {
				
				// Instantiate into container:
				if (numEntries == 0) {
					entryContainer.gameObject.SetActive(true);
					if (entryDescription != null) entryDescription.gameObject.SetActive(false);
					alternateEntryDescriptions.SetActive(false);
				}
				switch (entryState) {
				case QuestState.Active:
					InstantiateFirstValidTextElement(text, entryContainer, entryDescription);
					break;
				case QuestState.Success:
					InstantiateFirstValidTextElement(text, entryContainer, alternateEntryDescriptions.successDescription, entryDescription);
					break;
				case QuestState.Failure:
					InstantiateFirstValidTextElement(text, entryContainer, alternateEntryDescriptions.failureDescription, entryDescription);
					break;
				}
			}
			numEntries++;
		}
		
		private void InstantiateFirstValidTextElement(string text, Transform container, params UnityEngine.UI.Text[] textElements) {
			for (int i = 0; i < textElements.Length; i++) {
				if (textElements[i] != null) {
					var instance = Instantiate(textElements[i].gameObject) as GameObject;
					instance.transform.SetParent(container.transform, false);
					instance.SetActive(true);
					var textElement = instance.GetComponent<UnityEngine.UI.Text>();
					if (textElement != null) textElement.text = text;
					return;
				}
			}
		}
		
	}

}
#endif