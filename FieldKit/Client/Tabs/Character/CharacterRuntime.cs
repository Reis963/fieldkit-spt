namespace FieldKit
{
    public sealed partial class Plugin
    {
        private void UpdateCharacterTools()
        {
            ActiveHealthController health =
                _localPlayer == null
                    ? null
                    : _localPlayer.ActiveHealthController;

            if (health == null)
            {
                _lastCharacterRecoveryTime = 0f;
                _nextCharacterRecoveryTime = 0f;
                return;
            }

            float now = Time.unscaledTime;
            if (now < _nextCharacterRecoveryTime)
                return;

            float elapsed = _lastCharacterRecoveryTime <= 0f
                ? 0.1f
                : Mathf.Clamp(
                    now - _lastCharacterRecoveryTime,
                    0.01f,
                    0.5f);
            _lastCharacterRecoveryTime = now;
            _nextCharacterRecoveryTime = now + 0.1f;

            float recovery = _healthRegeneration.Value * elapsed;
            if (recovery <= 0f)
                return;

            for (int i = 0; i < 7; i++)
            {
                EBodyPart bodyPart = (EBodyPart)i;
                if (health.IsBodyPartDestroyed(bodyPart))
                    continue;

                ValueStruct value =
                    health.GetBodyPartHealth(bodyPart, false);
                float amount = Mathf.Min(
                    recovery,
                    value.Maximum - value.Current);

                if (amount > 0f)
                {
                    health.ChangeHealth(
                        bodyPart,
                        amount,
                        default);
                }
            }
        }
    }
}
