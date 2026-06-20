using HRMS.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HRMS.Pages;

public class MemorandumsModel : PageModel
{
    private readonly MemorandumService _memorandums;

    public MemorandumsModel(MemorandumService memorandums)
    {
        _memorandums = memorandums;
    }

    public string PageTitle => "Memorandums";
    public List<MemorandumItem> ActiveMemorandums { get; set; } = new();
    public MemorandumItem? Selected { get; set; }

    public void OnGet(int? id)
    {
        ActiveMemorandums = _memorandums.GetActiveMemorandums();

        if (id.HasValue && id > 0)
            Selected = _memorandums.GetById(id.Value);
        else if (ActiveMemorandums.Count > 0)
            Selected = ActiveMemorandums[0];
    }
}
