namespace CrowdCombat.Core
{
    /// <summary>
    /// Tick 시스템에서 호출될 수 있는 객체를 위한 인터페이스.
    /// TickManager에서 분산 호출되어 성능을 최적화합니다.
    /// </summary>
    public interface ITickable
    {
        /// <summary>
        /// Tick 호출 시 실행될 AI 로직.
        /// </summary>
        void Tick();
    }
}
