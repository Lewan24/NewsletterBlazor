using Microsoft.AspNetCore.Identity;

namespace NewsletterBlazor.Data.Entities;

public class Pagination
{
    protected List<LinkModel> links = new();
    public List<LinkModel> Links { get => links; }

    protected List<IdentityUser> actualPageUsers = new();
    public List<IdentityUser> ActualPageUsers { get => actualPageUsers; }

    protected int numberOfPages, selectedPage;

    public int NumberOfPages { get => numberOfPages; }
    public int SelectedPage { get => selectedPage; }
    public int UsersOnOnePage { get; set; }

    public string? IsPreviousDisabledString { get; set; } = "";
    public string? IsNextDisabledString { get; set; } = "";

    protected bool isReadyToWork = false;
    public bool IsReadyToWork { get => isReadyToWork; }

    public Pagination()
    {
        UsersOnOnePage = 5;
    }
    public Pagination(int HowManyOrdersForOnePage, bool NoLimit = false)
    {
        if (!NoLimit)
        {
            UsersOnOnePage = HowManyOrdersForOnePage;
            return;
        }

        UsersOnOnePage = 10_000;
    }

    public async Task LoadSettings(List<IdentityUser> Users)
    {
        numberOfPages = LoadPages(Users.Count);

        links.Clear();
        for (int i = 0; i < NumberOfPages; i++)
            links.Add(new LinkModel() { ID = i });

        await Task.Run(() => ChangeSelectedPage(0, Users));

        if (!isReadyToWork)
            isReadyToWork = true;
    }
    protected int LoadPages(int ListItemsCount)
    {
        int actualPage = 0;
        int howManyUsers = ListItemsCount;

        while (howManyUsers > UsersOnOnePage)
        {
            howManyUsers -= UsersOnOnePage;
            actualPage++;
        }

        return actualPage + 1;
    }
    public async Task ChangeSelectedPage(int page, List<IdentityUser> Users)
    {
        if (page >= 0 & page < NumberOfPages)
            selectedPage = page;

        int index = SelectedPage == 0 ? 0 : SelectedPage * UsersOnOnePage;

        actualPageUsers = Users.Count >= index + UsersOnOnePage
            ? Users.GetRange(index, UsersOnOnePage).ToList()
            : Users.GetRange(index, Users.Count - index).ToList();

        await Task.Run(CheckAndChangeStrings);
    }
    protected void CheckAndChangeStrings()
    {
        IsPreviousDisabledString = SelectedPage == 0 ? "disabled" : "";

        IsNextDisabledString = SelectedPage == NumberOfPages - 1 ? "disabled" : "";

        foreach (var link in Links)
        {
            link.IsActiveString = link.ID == SelectedPage ? "active" : "";
        }
    }
}

/*
 * Create a new instance of Pagination
 * In razor file foreach Link show a nav link that @onclicks goes to ChangeSelectedPage(Link.ID, List_of_Items)
 * Create own table with desiag as what you want
 * Foreach item in ActualPageUsers show item like the way you want
 * 
 * Remember that you need to LoadSettings() every time when you want to rerender page or initialize it
 */

// Example of navbar using Pagination class and Bootstrap 5.0

//@if(pagination.IsReadyToWork)
//{
//  <nav aria-label="Page navigation example">
//       <ul class="pagination justify-content-center flex-wrap">
//           <li class="page-item @pagination.IsPreviousDisabledString">
//               <a class="page-link" @onclick="() => pagination.ChangeSelectedPage(pagination.SelectedPage-1, Users)">Poprzednia</a>
//           </li>

//           @foreach(var link in pagination.Links)
//           {
//               <li class="page-item @link.IsActiveString">
//                  <a class="page-link" @onclick="() => pagination.ChangeSelectedPage(link.ID, Users)">@(link.ID+1)</a>
//               </li>
//           }

//           <li class="page-item @pagination.IsNextDisabledString" >
//              <a class= "page-link" @onclick="() => pagination.ChangeSelectedPage(pagination.SelectedPage+1, Users)">Następna</a>
//            </li>
//        </ul>
//  </nav>
//}