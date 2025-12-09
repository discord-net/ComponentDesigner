using Discord;
using static Discord.ComponentDesigner;

namespace Sandbox.Examples.Spyfall;

public class PackExample
{
    public static CXMessageComponent CreatePackInfo(
        Pack pack,
        int locationPage
    ) => cx(
        $"""
         <container>
             <PackHeader pack={pack} />
             <PackAuthor user={pack.Author} />
             <separator spacing="large" />
             <Locations locations={pack.Locations} page={locationPage}/>
             <PageControls pack={pack} page={locationPage} />
         </container>
         """
    );

    public static CXMessageComponent PageControls(Pack pack, int page)
        => cx(
            $"""
            <row>
                <button 
                    customId="page-{page - 1}-{pack.Id}"
                    style="primary" 
                    disabled={page is 0}
                >
                    Previous
                </button>
                <button
                    customId="page-{page + 1}-{pack.Id}"
                    style="primary"
                    disabled={pack.Locations.Count <= page}
                >  
                    Next
                </button>
            </row>
            """
        );

    public static CXMessageComponent Locations(IReadOnlyList<Location> locations, int page)
    {
        const int packLocationsPerPage = 3;

        var displayedLocations = locations
            .Skip(page * packLocationsPerPage)
            .Take(packLocationsPerPage)
            .ToArray();

        var pageNumber = page + 1;
        var pageLower = (page * packLocationsPerPage) + 1;
        var pageUpper = pageLower + displayedLocations.Length;

        return cx(
            $"""
             <text>
                 ## {locations.Count} Locations
                 -# Showing page {pageNumber}, locations {pageLower} to {pageUpper}
             </text>
             
             {displayedLocations.Select((x, i) => LocationListItem(x, i, pageLower))}
             """
        );
    }

    public static CXMessageComponent LocationListItem(Location location, int index, int pageLower)
    {
        var content = cx(
            $"""
             <text>
                 ### {index + pageLower}. {location.Name}
                 {
                     string.Join(
                         Environment.NewLine,
                         location.Roles.Select(x => $"- {x.Name} ({x.Chance}%)")
                     )
                 }
             </text>
             """
        );

        if (location.Icon is not null)
            content = cx(
                $"""
                 <section
                     accessory=(
                        <thumbnail url={location.Icon} />
                     )
                 >
                     {content}
                 </section>
                 """
            );

        return cx(
            $"""
             <separator />
             {content}
             """
        );
    }

    public static CXMessageComponent PackAuthor(User? user)
    {
        if (user is null) return CXMessageComponent.Empty;

        var viewProfileButton = cx(
            $"""
             <button
                 customId="view-profile-{user.Id}"
                 style="secondary"
             >
                 View Profile
             </button>
             """
        );

        return cx(
            $"""
             <section accessory={viewProfileButton}>
                 <text>
                     Created by {user.DisplayName}
                 </text>
             </section>
             """
        );
    }

    public static CXMessageComponent PackHeader(Pack pack)
    {
        var packHeaderText = cx(
            $"""
             <text>
                 # {pack.Name}
                 {pack.Description}
             </text>
             """
        );

        if (pack.Icon is null) return packHeaderText;

        return cx(
            $"""
             <section
                accessory=(
                    <thumbnail url={pack.Icon} />
                )
             >
                 {packHeaderText}
             </section>
             """
        );
    }
}