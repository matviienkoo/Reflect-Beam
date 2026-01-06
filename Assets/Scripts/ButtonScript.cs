using UnityEngine;
using UnityEngine.UI;

public class ButtonScript : MonoBehaviour
{
    public Button button; // Ссылка на кнопку
    private Vector3 originalScale; // Оригинальный масштаб кнопки
    private bool isAnimating = false; // Флаг, что анимация в процессе
    private float animationTime = 0.1f; // Время анимации
    private float timer = 0f; // Таймер для анимации
    private Vector3 targetScale; // Целевой масштаб кнопки
    private float scaleFactor = 0.8f; // Во сколько раз увеличиваем кнопку при клике

    void Start()
    {
        // Сохраняем оригинальный масштаб кнопки
        originalScale = button.transform.localScale;

        // Добавляем слушатель на событие клика
        button.onClick.AddListener(OnButtonClick);
    }

    void Update()
    {
        // Если анимация в процессе
        if (isAnimating)
        {
            // Увеличиваем таймер
            timer += Time.deltaTime;

            // Интерполируем между оригинальным и целевым масштабом
            button.transform.localScale = Vector3.Lerp(targetScale, originalScale, timer / animationTime);

            // Если время анимации истекло, завершаем анимацию
            if (timer >= animationTime)
            {
                isAnimating = false;
                button.transform.localScale = originalScale;
            }
        }
    }

    void OnButtonClick()
    {
        // Начинаем анимацию при клике
        if (!isAnimating)
        {
            // Устанавливаем целевой масштаб
            targetScale = originalScale * scaleFactor;
            
            // Применяем целевой масштаб мгновенно (эффект нажатия)
            button.transform.localScale = targetScale;

            // Сбрасываем таймер
            timer = 0f;

            // Устанавливаем флаг анимации в true
            isAnimating = true;
        }
    }
}