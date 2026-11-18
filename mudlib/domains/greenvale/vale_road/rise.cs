// @inherit /std/room

public class VrRise : Room
{
    public override void Create()
    {
        ShortDesc = "Vale Road, the Rise";
        LongDesc = "The road climbs a long, low hill, and at the top the whole of Oakenfell opens before you. A grey curtain of walls runs across the plain, studded with towers, with the River Mere shining past its eastern flank under a stone bridge. Behind the walls roofs pile on roofs in red tile and weathered slate, and above them all rise the great timber-and-stone roofs of the Hall of Legends, a hundred gables seeming to join into one. Smoke from a thousand hearths drifts up, bells carry faintly on the wind, and the road below is thick with carts. After the single street of Vincent's Hollow, the sight takes your breath away.";
        Exits["south"] = "/domains/greenvale/vale_road/river_bend";
        Exits["north"] = "/domains/greenvale/vale_road/approach";
        ResetSeconds = 120;

        AddDetail("city,oakenfell,town", "Oakenfell fills the plain from the river to the western fields: thousands of roofs, hundreds of chimneys, towers, domes and spires, with the road running straight to its southern gate. Nowhere in the valley is there anything of such size.");
        AddDetail("walls,wall,towers,tower", "The city walls are grey stone, thirty feet high, with square towers at intervals. They are old, mended in many shades of stone and twice rebuilt, and above the parapet you can see the glint of helms.");
        AddDetail("hall,hall of legends,roofs,roof,gables", "Above the other roofs rises the vast bulk of the Hall of Legends. It looks like a dozen halls grown together over centuries, timber wings and stone towers and a long slate roof under a bronze-green dome, and every gable is topped with a banner.");
        AddDetail("river,mere,river mere,bridge", "The River Mere curls past the city's eastern side, wide and busy with barges, and a long stone bridge carries the eastern road over it.");
        AddDetail("carts,traffic,road", "Carts, wagons and strings of pack mules crowd the road below, and walkers thread between them. The traffic thickens toward the gate, and you can hear the cries of drovers from here.");
        AddDetail("smoke,bells", "A haze of woodsmoke hangs above the rooftops, gold in the light. Under it, thin and sweet, you hear bells ringing the hour.");

    }
}
