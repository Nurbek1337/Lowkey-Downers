ИСПРАВЛЕНИЕ ОШИБОК КОМПИЛЯЦИИ

Этот архив является дополнительным патчем к LowkeyDowners_8PlayableCharacters_Patch.zip.
Он не является новым Unity-проектом.

Установка:
1. Закройте Unity или дождитесь окончания импорта.
2. Распакуйте содержимое архива в корень существующего проекта Lowkey Downers.
3. Подтвердите замену файлов.
4. Вернитесь в Unity и дождитесь компиляции.

Исправлено:
- ParrySystem получил allowPlayerInput и StartParryForAI.
- Парирование поддерживает ручное управление и вызов союзником-ИИ.
- Убраны устаревшие параметры FindObjectsByType(..., FindObjectsSortMode.None) в CharacterPartyManager и CharacterAbilityController.

Важно: патч заменяет только ParrySystem.cs, CharacterPartyManager.cs и CharacterAbilityController.cs.
Существующие сцены, боссы и остальные файлы проекта не меняются.
