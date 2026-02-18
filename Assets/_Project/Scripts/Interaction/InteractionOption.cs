using System;

public struct InteractionOption
{
    public string title;           // UI에 보일 문구 ("대화하기", "선물 주기")
    public bool enabled;           // 가능/불가
    public string reason;          // 불가 사유
    public Action execute;         // 실행 로직

    public InteractionOption(string title, Action execute, bool enabled = true, string reason = null)
    {
        this.title = title;
        this.execute = execute;
        this.enabled = enabled;
        this.reason = reason;
    }
}
