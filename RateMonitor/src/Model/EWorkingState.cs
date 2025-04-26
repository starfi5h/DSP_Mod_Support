namespace RateMonitor.Model
{
    public enum EWorkingState
    {
        Running = 0, // 正常运转
        Inefficient = 1, //低效            
        Idle = 2, // 停止运转 (待机)
        Removed = 3, // 无法监控
        Full = 4, // 产物堆积
        Lack = 5, // 缺少原材料            
        LackInc = 6, // 缺少增产剂
        LackMatrix = 7, // 矩阵不足
        MinerSlowMode = 8, // 減速模式(礦機)
        EjectorNoOrbit = 9, // 轨道未设置
        EjectorBlocked = 10, // 路径被遮挡
        EjectorAngleLimit = 11, // 俯仰限制
        SiloNoNode = 12, // 待机无需求
        GammaNoLens = 13, //鍋:沒有透鏡
        GammaWarmUp = 14, //鍋:熱機中
        NeedFuel = 15, //需要燃料
        Error, // 在計算過程中出錯
        Max
    }
}
