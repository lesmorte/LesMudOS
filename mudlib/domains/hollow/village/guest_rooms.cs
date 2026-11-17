// @inherit /std/room

/// <summary>Safe room, logout room and the place players wake after dying.</summary>
public class GuestRooms : Room
{
    public override void Create()
    {
        ShortDesc = "Guest Rooms";
        LongDesc = "Several modest guest rooms sit above the tavern. Clean beds and warm blankets provide a welcome refuge for weary travelers.";
        Exits["down"] = "/domains/hollow/village/tavern";
        Safe = true;
        LogoutRoom = true;

        AddDetail("beds,bed,blankets,blanket", "Narrow beds with clean linen and thick wool blankets. They look very inviting.");
        AddDetail("rooms,guest rooms", "The doors stand open. Each little room holds a bed, a washstand and a window that looks out over the palisade.");
    }
}
