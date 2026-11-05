using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

public sealed class LevelUpUI : MonoBehaviour
{
    [SerializeField] private GameObject overlayRoot;
    [FormerlySerializedAs("attackButton"), SerializeField] private Button firstButton;
    [FormerlySerializedAs("healthButton"), SerializeField] private Button secondButton;
    [FormerlySerializedAs("speedButton"), SerializeField] private Button thirdButton;

    private PlayerProgress progress;
    private PlayerAugments augments;
    private Font runtimeFont;
    private Button[] buttons;
    private Vector2[] cardPositions;
    private AugmentId[] offers;
    private int pendingChoices;
    private float timeScaleBeforeOpen = 1f;

    public bool IsOpen => pendingChoices > 0;

    public void Configure(GameObject overlay, Button first, Button second, Button third)
    {
        overlayRoot = overlay;
        firstButton = first;
        secondButton = second;
        thirdButton = third;
    }

    private void Awake()
    {
        progress = FindFirstObjectByType<PlayerProgress>();
        augments = FindFirstObjectByType<PlayerAugments>();
        buttons = new[] { firstButton, secondButton, thirdButton };
        cardPositions = new Vector2[buttons.Length];
        for (int index = 0; index < buttons.Length; index++)
            cardPositions[index] = ((RectTransform)buttons[index].transform).anchoredPosition;
        overlayRoot.SetActive(false);

        runtimeFont = ProjectKoreanFont.Load();
        if (runtimeFont != null)
        {
            foreach (Text label in GetComponentsInChildren<Text>(true))
                label.font = runtimeFont;
        }

        if (progress != null)
            progress.LeveledUp += OnLeveledUp;
        firstButton.onClick.AddListener(ChooseFirst);
        secondButton.onClick.AddListener(ChooseSecond);
        thirdButton.onClick.AddListener(ChooseThird);
    }

    private void OnLeveledUp(int level)
    {
        if (pendingChoices == 0)
        {
            timeScaleBeforeOpen = Time.timeScale;
            Time.timeScale = 0f;
            RollAndShowOffers();
            overlayRoot.SetActive(true);
        }
        pendingChoices++;
    }

    private void RollAndShowOffers()
    {
        if (augments == null)
            augments = FindFirstObjectByType<PlayerAugments>();
        offers = augments.RollOffers();
        for (int index = 0; index < buttons.Length; index++)
        {
            bool available = index < offers.Length;
            buttons[index].gameObject.SetActive(available);
            RectTransform card = (RectTransform)buttons[index].transform;
            card.anchoredPosition = offers.Length == 2 && available
                ? cardPositions[1] + new Vector2((index == 0 ? -1f : 1f) * 255f, 0f)
                : cardPositions[index];
            if (!available)
                continue;

            AugmentDefinition definition = AugmentCatalog.Get(offers[index]);
            int currentLevel = augments.GetLevel(offers[index]);
            Text title = buttons[index].transform.Find("Title").GetComponent<Text>();
            Text description = buttons[index].transform.Find("Description").GetComponent<Text>();
            title.text = definition.Title;
            title.fontSize = 32;
            description.fontSize = 26;
            description.text = definition.LabelForNextLevel(currentLevel) + "\n" +
                               definition.DescriptionForNextLevel(currentLevel);
        }
    }

    private void Update()
    {
        if (!IsOpen || Keyboard.current == null)
            return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame)
            Choose(0);
        else if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame)
            Choose(1);
        else if (Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame)
            Choose(2);
    }

    private void ChooseFirst() => Choose(0);
    private void ChooseSecond() => Choose(1);
    private void ChooseThird() => Choose(2);

    private void Choose(int index)
    {
        if (!IsOpen || offers == null || augments == null || index < 0 || index >= offers.Length)
            return;
        augments.Apply(offers[index]);
        pendingChoices--;
        if (pendingChoices > 0)
        {
            RollAndShowOffers();
            return;
        }

        offers = null;
        overlayRoot.SetActive(false);
        Time.timeScale = timeScaleBeforeOpen;
    }

    private void OnDestroy()
    {
        if (progress != null)
            progress.LeveledUp -= OnLeveledUp;
        if (firstButton != null)
            firstButton.onClick.RemoveListener(ChooseFirst);
        if (secondButton != null)
            secondButton.onClick.RemoveListener(ChooseSecond);
        if (thirdButton != null)
            thirdButton.onClick.RemoveListener(ChooseThird);
        if (IsOpen)
            Time.timeScale = timeScaleBeforeOpen;
    }
}
