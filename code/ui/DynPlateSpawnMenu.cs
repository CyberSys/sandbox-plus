using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using MinimalExtended;
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using Sandbox.UI.Tests;

namespace Sandbox
{
	[Library]
	public class DynPlateSpawnMenu : IAutoload
	{
		public bool ReloadOnHotload => false;
		public DynPlateSpawnMenu()
		{
			if ( Host.IsClient ) {
				Log.Info( "[Client] DynPlateSpawnMenu created" );

				SandboxHud.OnHudLoaded -= Initialize;
				SandboxHud.OnHudLoaded += Initialize;
			}
		}
		public void Initialize()
		{
			var plateList = SpawnMenu.Instance.SpawnMenuLeftBody.AddChild<PlateList>();
			SpawnMenu.Instance.SpawnMenuLeftTabs
				.AddButtonActive( "DynPlates", ( b ) => plateList.SetClass( "active", b ) );
		}

		public void Dispose()
		{
		}
	}

	[Library]
	public partial class PlateList : Panel
	{
		private struct DynPlateDimensions
		{
			[JsonInclude]
			public int x;
			[JsonInclude]
			public int y;
			[JsonInclude]
			public int z;
			[JsonInclude]
			public int scale;
			public DynPlateDimensions( int x, int y, int z, int scale )
			{
				this.x = x;
				this.y = y;
				this.z = z;
				this.scale = scale;
			}
		}
		private VirtualScrollPanel Canvas;
		private HashSet<DynPlateDimensions> plates = new();

		public PlateList()
		{
			AddClass( "dynplates" );
			AddChild( out Canvas, "canvas" );

			Canvas.Layout.Columns = 16;
			Canvas.Layout.ItemSize = new Vector2( 64, 64 );
			Canvas.OnCreateCell = ( cell, data ) => {
				var entry = (DynPlateDimensions)data;
				var btn = cell.Add.Button( $"{entry.x}x{entry.y}" );
				btn.AddClass( "icon" );
				btn.AddEventListener( "onclick", () =>
					ConsoleSystem.Run( "spawn_dynplate", entry.x, entry.y, entry.z, entry.scale )
				);
			};

			var db = new Save.JsonSaveModule( "dynplate_spawnlist", true );
			var storedSpawnListSerialized = db.LoadClass<string>( "plates" );
			if ( storedSpawnListSerialized != null ) {
				plates = JsonSerializer.Deserialize<HashSet<DynPlateDimensions>>( storedSpawnListSerialized );
			}
			else {
				plates.Add( new DynPlateDimensions( 12, 12, 1, 64 ) );
				plates.Add( new DynPlateDimensions( 12, 24, 1, 64 ) );
				plates.Add( new DynPlateDimensions( 24, 24, 1, 64 ) );
				plates.Add( new DynPlateDimensions( 24, 48, 1, 64 ) );
				plates.Add( new DynPlateDimensions( 48, 48, 1, 64 ) );
				plates.Add( new DynPlateDimensions( 48, 96, 1, 64 ) );
				plates.Add( new DynPlateDimensions( 96, 96, 1, 64 ) );
			}

			Canvas.SetItems( GetOrderedPlates() );

			var inputContainer = Add.Panel( "input-container" );
			var dimensionsContainer = inputContainer.Add.Panel( "dimensions-container" );
			dimensionsContainer.Add.Label( "Rectangles", "header" );

			var lengthContainer = dimensionsContainer.Add.Panel( "flex-column" );
			lengthContainer.Add.Label( "Length", "input-label" );
			var lengthEntry = lengthContainer.Add.MenuTextEntry( "48" );

			var widthContainer = dimensionsContainer.Add.Panel( "flex-column" );
			widthContainer.Add.Label( "Width", "input-label" );
			var widthEntry = widthContainer.Add.MenuTextEntry( "24" );

			var heightContainer = dimensionsContainer.Add.Panel( "flex-column" );
			heightContainer.Add.Label( "Height", "input-label" );
			var heightEntry = heightContainer.Add.MenuTextEntry( "3" );

			var textureSizeContainer = dimensionsContainer.Add.Panel( "flex-column" );
			textureSizeContainer.Add.Label( "Texture Size", "input-label" );
			var textureSizeEntry = textureSizeContainer.Add.MenuTextEntry( "64" );

			var spawnButton = dimensionsContainer.Add.Button( "Spawn DynPlate", () => {
				if ( !int.TryParse( lengthEntry.Text, out int x )
					|| !int.TryParse( widthEntry.Text, out int y )
					|| !int.TryParse( heightEntry.Text, out int z )
					|| !int.TryParse( textureSizeEntry.Text, out int texSize ) ) {
					return;
				}
				ConsoleSystem.Run( "spawn_dynplate", x, y, z, texSize );
				var added = plates.Add( new DynPlateDimensions( x, y, z, texSize ) );
				if ( added ) {
					db.SaveClass( "plates", JsonSerializer.Serialize( plates ) );
					Canvas.SetItems( GetOrderedPlates() );
				}
			} );
		}

		private IEnumerable<object> GetOrderedPlates()
		{
			return plates.OrderBy( plate => plate.x )
				.ThenBy( plate => plate.y )
				.ThenBy( plate => plate.z )
				.ThenBy( plate => plate.scale )
				.Select( plate => (object)plate );
		}
	}
}
