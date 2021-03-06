// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing.Design;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using Engine;
using Engine.Renderer;
using Engine.MathEx;
using Engine.MapSystem;
using Engine.FileSystem;
using Engine.Utils;

namespace ProjectCommon
{
	/// <summary>
	/// Base template for shader materials.
	/// </summary>
	[Description( "A base material class in the engine." )]
	public class ShaderBaseMaterial : HighLevelMaterial
	{
		//General
		Range fadingByDistanceRange;
		float softParticlesFadingLength = 1;
		float depthOffset;

		//Diffuse
		ColorValue diffuseColor = new ColorValue( 1, 1, 1 );
		float diffusePower = 1;

		//Reflection
		ColorValue reflectionColor = new ColorValue( 0, 0, 0 );
		float reflectionPower = 1;

		//Emission
		ColorValue emissionColor = new ColorValue( 0, 0, 0 );
		float emissionPower = 1;

		//Specular
		ColorValue specularColor = new ColorValue( 0, 0, 0 );
		float specularPower = 1;
		float specularShininess = 20;

		//Translucency
		ColorValue translucencyColor = new ColorValue( 0, 0, 0 );
		float translucencyPower = 1;
		float translucencyClearness = 4f;

		//Height
		float heightScale = .04f;

		//Projective texturing
		bool projectiveTexturing;
		RenderFrustum projectiveTexturingFrustum;

		//for cubemap reflections
		List<Pair<Pass, TextureUnitState>> cubemapEventUnitStates;

		//for maps animations
		List<MapItem> mapsWithAnimations;

		List<Pass> subscribedPassesForRenderObjectPass;

		string defaultTechniqueErrorString;
		bool fixedPipelineInitialized;
		bool emptyMaterialInitialized;

		static float mapTransformAnimationTime;
		static float mapTransformAnimationTimeLastFrameRenderTime;

		///////////////////////////////////////////

		//gpu parameters constants
		public enum GpuParameters
		{
			dynamicDiffuseScale = 1,
			dynamicEmissionScale,
			dynamicReflectionScale,
			dynamicSpecularScaleAndShininess,

			fadingByDistanceRange,
			softParticlesFadingLength,
			depthOffset,
			heightScale,

			diffuse1MapTransformMul,
			diffuse1MapTransformAdd,
			diffuse2MapTransformMul,
			diffuse2MapTransformAdd,
			diffuse3MapTransformMul,
			diffuse3MapTransformAdd,
			diffuse4MapTransformMul,
			diffuse4MapTransformAdd,
			reflectionMapTransformMul,
			reflectionMapTransformAdd,
			emissionMapTransformMul,
			emissionMapTransformAdd,
			specularMapTransformMul,
			specularMapTransformAdd,
			translucencyMapTransformMul,
			translucencyMapTransformAdd,
			translucencyScaleAndClearness,
			normalMapTransformMul,
			normalMapTransformAdd,
			heightMapTransformMul,
			heightMapTransformAdd,

			texViewProjImageMatrix0,
			texViewProjImageMatrix1,
			texViewProjImageMatrix2,
			texViewProjImageMatrix3,

			LastIndex,
		}

		///////////////////////////////////////////

		public enum MaterialBlendingTypes
		{
			Opaque,
			AlphaAdd,
			AlphaBlend,
		}

		///////////////////////////////////////////

		public enum TexCoordIndexes
		{
			TexCoord0,
			TexCoord1,
			TexCoord2,
			TexCoord3,
			Projective,
		}

		///////////////////////////////////////////

		public enum DisplacementTechniques
		{
			ParallaxMapping,
			ParallaxOcclusionMapping,
		}

		///////////////////////////////////////////

		//for expand properties and allow change texture name from the group textbox in the propertyGrid
		public class MapItemTypeConverter : ExpandableObjectConverter
		{
			public override bool CanConvertFrom( ITypeDescriptorContext context, Type sourceType )
			{
				return sourceType == typeof( string );
			}

			public override object ConvertFrom( ITypeDescriptorContext context,
				System.Globalization.CultureInfo culture, object value )
			{
				if( value.GetType() == typeof( string ) )
				{
					var property = typeof( ShaderBaseMaterial ).GetProperty(
						context.PropertyDescriptor.Name );
					var map = (MapItem)property.GetValue( context.Instance, null );
					map.Texture = (string)value;
					return map;
				}
				return base.ConvertFrom( context, culture, value );
			}
		}

		///////////////////////////////////////////

		//special EditorTextureUITypeEditor for MapItem classes
		public class MapItemEditorTextureUITypeEditor : UITypeEditor
		{
			public override object EditValue( ITypeDescriptorContext context,
				IServiceProvider provider, object value )
			{
				var map = (MapItem)value;

				var path = map.Texture;
				if( ResourceUtils.DoUITypeEditorEditValueDelegate( "Texture", ref path, null, true ) )
				{
					if( path == null )
						path = "";

					//create new MapItem and copy properties.
					//it is need for true property grid updating.
					var type = map.GetType();
					var constructor = type.GetConstructor(
						BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
						null, new[] { typeof( ShaderBaseMaterial ) }, null );
					var newMap = (MapItem)constructor.Invoke( new object[] { map.owner } );
					newMap.OnClone( map );

					newMap.Texture = path;

					return newMap;
				}

				return value;
			}

			public override UITypeEditorEditStyle GetEditStyle( ITypeDescriptorContext context )
			{
				return UITypeEditorEditStyle.Modal;
			}
		}

		///////////////////////////////////////////

		[TypeConverter( typeof( MapItemTypeConverter ) )]
		[Editor( typeof( MapItemEditorTextureUITypeEditor ), typeof( UITypeEditor ) )]
		public class MapItem
		{
			internal ShaderBaseMaterial owner;

			internal List<TextureUnitState> textureUnitStatesForFixedPipeline;

			//

			internal MapItem( ShaderBaseMaterial owner )
			{
				this.owner = owner;
				Transform = new TransformItem( this );
			}

			[DefaultValue( "" )]
			[Editor( typeof( EditorTextureUITypeEditor ), typeof( UITypeEditor ) )]
			[SupportRelativePath]
			[RefreshProperties( RefreshProperties.Repaint )]
			[LocalizedDisplayName( "Texture", "ShaderBaseMaterial" )]
			public string Texture { get; set; } = "";

			public string GetTextureFullPath()
			{
				if( string.IsNullOrEmpty( owner.FileName ) )
					return Texture;
				return RelativePathUtils.ConvertToFullPath( Path.GetDirectoryName( owner.FileName ), Texture );
			}

			[DefaultValue( TexCoordIndexes.TexCoord0 )]
			[RefreshProperties( RefreshProperties.Repaint )]
			[LocalizedDisplayName( "TexCoord", "ShaderBaseMaterial" )]
			public TexCoordIndexes TexCoord { get; set; } = TexCoordIndexes.TexCoord0;

			[DefaultValue( false )]
			[RefreshProperties( RefreshProperties.Repaint )]
			[LocalizedDisplayName( "Clamp", "ShaderBaseMaterial" )]
			public bool Clamp { get; set; }

			[LocalizedDisplayName( "Transform", "ShaderBaseMaterial" )]
			public TransformItem Transform { get; set; }

			public override string ToString()
			{
				if( string.IsNullOrEmpty( Texture ) )
					return "";
				return Texture;
			}

			public virtual void Load( TextBlock block )
			{
				if( block.IsAttributeExist( "texture" ) )
					Texture = block.GetAttribute( "texture" );

				if( block.IsAttributeExist( "texCoord" ) )
					TexCoord = (TexCoordIndexes)Enum.Parse( typeof( TexCoordIndexes ),
						block.GetAttribute( "texCoord" ) );

				if( block.IsAttributeExist( "clamp" ) )
					Clamp = bool.Parse( block.GetAttribute( "clamp" ) );

				var transformBlock = block.FindChild( "transform" );
				if( transformBlock != null )
					Transform.Load( transformBlock );
			}

			public virtual void Save( TextBlock block )
			{
				if( !string.IsNullOrEmpty( Texture ) )
					block.SetAttribute( "texture", Texture );

				if( TexCoord != TexCoordIndexes.TexCoord0 )
					block.SetAttribute( "texCoord", TexCoord.ToString() );

				if( Clamp )
					block.SetAttribute( "clamp", Clamp.ToString() );

				if( Transform.IsDataExists() )
				{
					var transformBlock = block.AddChild( "transform" );
					Transform.Save( transformBlock );
				}
			}

			public virtual bool IsDataExists()
			{
				return !string.IsNullOrEmpty( Texture ) || TexCoord != TexCoordIndexes.TexCoord0 ||
					Clamp || Transform.IsDataExists();
			}

			internal virtual void OnClone( MapItem source )
			{
				Texture = source.GetTextureFullPath();
				TexCoord = source.TexCoord;
				Clamp = source.Clamp;
				Transform.OnClone( source.Transform );
			}
		}

		///////////////////////////////////////////

		public class DiffuseMapItem : MapItem
		{
			public enum MapBlendingTypes
			{
				Add,
				Modulate,
				AlphaBlend,
			}

			internal DiffuseMapItem( ShaderBaseMaterial owner )
				: base( owner )
			{
			}

			[DefaultValue( MapBlendingTypes.Modulate )]
			[RefreshProperties( RefreshProperties.Repaint )]
			[LocalizedDisplayName( "Blending", "ShaderBaseMaterial" )]
			public MapBlendingTypes Blending { get; set; } = MapBlendingTypes.Modulate;

			public override void Load( TextBlock block )
			{
				base.Load( block );

				if( block.IsAttributeExist( "blending" ) )
					Blending = (MapBlendingTypes)Enum.Parse( typeof( MapBlendingTypes ),
						block.GetAttribute( "blending" ) );
			}

			public override void Save( TextBlock block )
			{
				base.Save( block );

				if( Blending != MapBlendingTypes.Modulate )
					block.SetAttribute( "blending", Blending.ToString() );
			}

			public override bool IsDataExists()
			{
				if( Blending != MapBlendingTypes.Modulate )
					return true;
				return base.IsDataExists();
			}

			internal override void OnClone( MapItem source )
			{
				base.OnClone( source );
				Blending = ( (DiffuseMapItem)source ).Blending;
			}
		}

		///////////////////////////////////////////

		[TypeConverter( typeof( ExpandableObjectConverter ) )]
		public class TransformItem
		{
			internal MapItem owner;
			Vec2 scroll;
			Vec2 scale = new Vec2( 1, 1 );
			float rotate;

			//

			internal TransformItem( MapItem owner )
			{
				this.owner = owner;
				Animation = new AnimationItem( this );
			}

			[DefaultValue( typeof( Vec2 ), "0 0" )]
			[Editor( typeof( Vec2ValueEditor ), typeof( UITypeEditor ) )]
			[EditorLimitsRange( -1, 1 )]
			[RefreshProperties( RefreshProperties.Repaint )]
			[LocalizedDisplayName( "Scroll", "ShaderBaseMaterial" )]
			public Vec2 Scroll
			{
				get => scroll;
				set
				{
					if( scroll == value )
						return;

					scroll = value;

					var map = owner;
					map.owner.UpdateMapTransformGpuParameters( map );

					if( map.owner.fixedPipelineInitialized )
						map.owner.UpdateMapTransformForFixedPipeline( map );
				}
			}

			[DefaultValue( typeof( Vec2 ), "1 1" )]
			[Editor( typeof( Vec2ValueEditor ), typeof( UITypeEditor ) )]
			[EditorLimitsRange( .1f, 30 )]
			[RefreshProperties( RefreshProperties.Repaint )]
			[LocalizedDisplayName( "Scale", "ShaderBaseMaterial" )]
			public Vec2 Scale
			{
				get => scale;
				set
				{
					if( scale == value )
						return;

					scale = value;

					var map = owner;
					map.owner.UpdateMapTransformGpuParameters( map );

					if( map.owner.fixedPipelineInitialized )
						map.owner.UpdateMapTransformForFixedPipeline( map );
				}
			}

			[DefaultValue( 0.0f )]
			[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
			[EditorLimitsRange( -1, 1 )]
			[RefreshProperties( RefreshProperties.Repaint )]
			[LocalizedDisplayName( "Rotate", "ShaderBaseMaterial" )]
			public float Rotate
			{
				get => rotate;
				set
				{
					if( rotate == value )
						return;

					rotate = value;

					var map = owner;
					map.owner.UpdateMapTransformGpuParameters( map );

					if( map.owner.fixedPipelineInitialized )
						map.owner.UpdateMapTransformForFixedPipeline( map );
				}
			}

			[DefaultValue( false )]
			public bool DynamicParameters { get; set; }

			[LocalizedDisplayName( "Animation", "ShaderBaseMaterial" )]
			public AnimationItem Animation { get; set; }

			public override string ToString()
			{
				var text = "";
				if( scroll != Vec2.Zero )
					text += $"Scroll: {scroll}";
				if( scale != new Vec2( 1, 1 ) )
				{
					if( text != "" )
						text += ", ";
					text += $"Scale: {scale}";
				}
				if( rotate != 0 )
				{
					if( text != "" )
						text += ", ";
					text += $"Rotate: {rotate}";
				}
				if( DynamicParameters )
				{
					if( text != "" )
						text += ", ";
					text += $"Dynamic Parameters: {DynamicParameters.ToString()}";
				}
				if( Animation.IsDataExists() )
				{
					if( text != "" )
						text += ", ";
					text += $"Animation: {Animation.ToString()}";
				}
				return text;
			}

			public void Load( TextBlock block )
			{
				if( block.IsAttributeExist( "scroll" ) )
					scroll = Vec2.Parse( block.GetAttribute( "scroll" ) );
				if( block.IsAttributeExist( "scale" ) )
					scale = Vec2.Parse( block.GetAttribute( "scale" ) );
				if( block.IsAttributeExist( "rotate" ) )
					rotate = float.Parse( block.GetAttribute( "rotate" ) );
				if( block.IsAttributeExist( "dynamicParameters" ) )
					DynamicParameters = bool.Parse( block.GetAttribute( "dynamicParameters" ) );

				var animationBlock = block.FindChild( "animation" );
				if( animationBlock != null )
					Animation.Load( animationBlock );
			}

			public void Save( TextBlock block )
			{
				if( scroll != Vec2.Zero )
					block.SetAttribute( "scroll", scroll.ToString() );
				if( scale != new Vec2( 1, 1 ) )
					block.SetAttribute( "scale", scale.ToString() );
				if( rotate != 0 )
					block.SetAttribute( "rotate", rotate.ToString() );
				if( DynamicParameters )
					block.SetAttribute( "dynamicParameters", DynamicParameters.ToString() );

				if( Animation.IsDataExists() )
				{
					var animationBlock = block.AddChild( "animation" );
					Animation.Save( animationBlock );
				}
			}

			public bool IsDataExists()
			{
				return scroll != Vec2.Zero || scale != new Vec2( 1, 1 ) ||
					rotate != 0 || DynamicParameters || Animation.IsDataExists();
			}

			internal void OnClone( TransformItem source )
			{
				scroll = source.scroll;
				scale = source.scale;
				rotate = source.rotate;
				DynamicParameters = source.DynamicParameters;
				Animation.OnClone( source.Animation );
			}
		}

		///////////////////////////////////////////

		[TypeConverter( typeof( ExpandableObjectConverter ) )]
		public class AnimationItem
		{
			internal TransformItem owner;
			Vec2 scrollSpeed;
			float rotateSpeed;

			internal AnimationItem( TransformItem owner )
			{
				this.owner = owner;
			}

			[DefaultValue( typeof( Vec2 ), "0 0" )]
			[Editor( typeof( Vec2ValueEditor ), typeof( UITypeEditor ) )]
			[EditorLimitsRange( -3, 3 )]
			[RefreshProperties( RefreshProperties.Repaint )]
			[LocalizedDisplayName( "ScrollSpeed", "ShaderBaseMaterial" )]
			public Vec2 ScrollSpeed
			{
				get => scrollSpeed;
				set
				{
					if( scrollSpeed == value )
						return;

					scrollSpeed = value;

					var map = owner.owner;
					map.owner.InitializeAndUpdateMapTransformGpuParameters( map );

					if( map.owner.fixedPipelineInitialized )
						map.owner.UpdateMapTransformForFixedPipeline( map );
				}
			}

			[DefaultValue( typeof( Vec2 ), "0 0" )]
			[Editor( typeof( Vec2ValueEditor ), typeof( UITypeEditor ) )]
			[EditorLimitsRange( 0, 1 )]
			[RefreshProperties( RefreshProperties.Repaint )]
			[LocalizedDisplayName( "ScrollRound", "ShaderBaseMaterial" )]
			public Vec2 ScrollRound { get; set; }

			[DefaultValue( 0.0f )]
			[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
			[EditorLimitsRange( -3, 3 )]
			[RefreshProperties( RefreshProperties.Repaint )]
			[LocalizedDisplayName( "RotateSpeed", "ShaderBaseMaterial" )]
			public float RotateSpeed
			{
				get => rotateSpeed;
				set
				{
					if( rotateSpeed == value )
						return;

					rotateSpeed = value;

					var map = owner.owner;
					map.owner.InitializeAndUpdateMapTransformGpuParameters( map );

					if( map.owner.fixedPipelineInitialized )
						map.owner.UpdateMapTransformForFixedPipeline( map );
				}
			}

			public override string ToString()
			{
				var text = "";
				if( scrollSpeed != Vec2.Zero )
					text += $"Scroll: {scrollSpeed}";
				if( rotateSpeed != 0 )
				{
					if( text != "" )
						text += ", ";
					text += $"Rotate: {rotateSpeed}";
				}
				return text;
			}

			public void Load( TextBlock block )
			{
				if( block.IsAttributeExist( "scrollSpeed" ) )
					scrollSpeed = Vec2.Parse( block.GetAttribute( "scrollSpeed" ) );
				if( block.IsAttributeExist( "scrollRound" ) )
					ScrollRound = Vec2.Parse( block.GetAttribute( "scrollRound" ) );
				if( block.IsAttributeExist( "rotateSpeed" ) )
					rotateSpeed = float.Parse( block.GetAttribute( "rotateSpeed" ) );
			}

			public void Save( TextBlock block )
			{
				if( scrollSpeed != Vec2.Zero )
					block.SetAttribute( "scrollSpeed", scrollSpeed.ToString() );
				if( ScrollRound != Vec2.Zero )
					block.SetAttribute( "scrollRound", ScrollRound.ToString() );
				if( rotateSpeed != 0 )
					block.SetAttribute( "rotateSpeed", rotateSpeed.ToString() );
			}

			public bool IsDataExists()
			{
				return scrollSpeed != Vec2.Zero || ScrollRound != Vec2.Zero || rotateSpeed != 0;
			}

			internal void OnClone( AnimationItem source )
			{
				scrollSpeed = source.scrollSpeed;
				ScrollRound = source.ScrollRound;
				rotateSpeed = source.rotateSpeed;
			}
		}

		///////////////////////////////////////////

		public static bool CreateEmptyMaterialsForFasterStartupInitialization { get; set; }

		public static void FinishInitializationOfEmptyMaterials()
		{
			var materials = new List<HighLevelMaterial>(
				HighLevelMaterialManager.Instance.Materials );
			foreach( var material in materials )
			{
				var shaderBaseMaterial = material as ShaderBaseMaterial;
				if( shaderBaseMaterial != null )
				{
					if( shaderBaseMaterial.emptyMaterialInitialized )
						shaderBaseMaterial.UpdateBaseMaterial();
				}
			}
		}

		///////////////////////////////////////////

		[Category( "_ShaderBase" )]
		[LocalizedDisplayName( "Blending", "ShaderBaseMaterial" )]
		[DefaultValue( MaterialBlendingTypes.Opaque )]
		public MaterialBlendingTypes Blending { get; set; }

		[Category( "_ShaderBase" )]
		[LocalizedDisplayName( "Lighting", "ShaderBaseMaterial" )]
		[DefaultValue( true )]
		public bool Lighting { get; set; } = true;

		[Category( "_ShaderBase" )]
		[LocalizedDisplayName( "AmbientLighting", "ShaderBaseMaterial" )]
		[DefaultValue( true )]
		public bool AmbientLighting { get; set; } = true;

		[Category( "_ShaderBase" )]
		[LocalizedDisplayName( "DoubleSided", "ShaderBaseMaterial" )]
		[DefaultValue( false )]
		public bool DoubleSided { get; set; }

		[Category( "_ShaderBase" )]
		[LocalizedDisplayName( "UseNormals", "ShaderBaseMaterial" )]
		[DefaultValue( true )]
		public bool UseNormals { get; set; } = true;

		[Category( "_ShaderBase" )]
		[LocalizedDisplayName( "ReceiveShadows", "ShaderBaseMaterial" )]
		[DefaultValue( true )]
		public bool ReceiveShadows { get; set; } = true;

		[Category( "_ShaderBase" )]
		[LocalizedDisplayName( "ReceiveSimpleShadows", "ShaderBaseMaterial" )]
		[DefaultValue( false )]
		public bool ReceiveSimpleShadows { get; set; }

		[Category( "_ShaderBase" )]
		[LocalizedDisplayName( "AlphaRejectFunction", "ShaderBaseMaterial" )]
		[DefaultValue( CompareFunction.AlwaysPass )]
		public CompareFunction AlphaRejectFunction { get; set; } = CompareFunction.AlwaysPass;

		[Category( "_ShaderBase" )]
		[LocalizedDisplayName( "AlphaRejectValue", "ShaderBaseMaterial" )]
		[DefaultValue( (byte)127 )]
		public byte AlphaRejectValue { get; set; } = 127;

		[Category( "_ShaderBase" )]
		[LocalizedDisplayName( "AlphaToCoverage", "ShaderBaseMaterial" )]
		[DefaultValue( false )]
		public bool AlphaToCoverage { get; set; }

		[Category( "_ShaderBase" )]
		[LocalizedDisplayName( "FadingByDistanceRange", "ShaderBaseMaterial" )]
		[DefaultValue( typeof( Range ), "0 0" )]
		public Range FadingByDistanceRange
		{
			get => fadingByDistanceRange;
			set
			{
				if( fadingByDistanceRange == value )
					return;
				fadingByDistanceRange = value;
				UpdateFadingByDistanceRangeGpuParameter();
			}
		}

		[Category( "_ShaderBase" )]
		[LocalizedDisplayName( "AllowFog", "ShaderBaseMaterial" )]
		[DefaultValue( true )]
		public bool AllowFog { get; set; } = true;

		[Category( "_ShaderBase" )]
		[LocalizedDisplayName( "DepthWrite", "ShaderBaseMaterial" )]
		[DefaultValue( true )]
		[Description( "Depth write flag will be automatically disabled if \"Blending\" not equal to \"Opaque\"." )]
		public bool DepthWrite { get; set; } = true;

		[Category( "_ShaderBase" )]
		[LocalizedDisplayName( "DepthTest", "ShaderBaseMaterial" )]
		[DefaultValue( true )]
		public bool DepthTest { get; set; } = true;

		[Category( "_ShaderBase" )]
		[DefaultValue( false )]
		public bool SoftParticles { get; set; }

		[Category( "_ShaderBase" )]
		[DefaultValue( 1.0f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( .1f, 10 )]
		public float SoftParticlesFadingLength
		{
			get => softParticlesFadingLength;
			set
			{
				if( softParticlesFadingLength == value )
					return;
				softParticlesFadingLength = value;
				UpdateSoftParticlesFadingLengthGpuParameter();
			}
		}

		[Category( "_ShaderBase" )]
		[DefaultValue( 0.0f )]
		public float DepthOffset
		{
			get => depthOffset;
			set
			{
				if( depthOffset == value )
					return;
				depthOffset = value;
				UpdateDepthOffsetGpuParameter();
			}
		}

		[Category( "_ShaderBase" )]
		[DefaultValue( false )]
		public bool HalfLambert { get; set; }

		///////////////////////////////////////////
		//Diffuse

		[Category( "Diffuse" )]
		[LocalizedDisplayName( "DiffuseColor", "ShaderBaseMaterial" )]
		[DefaultValue( typeof( ColorValue ), "255 255 255" )]
		public ColorValue DiffuseColor
		{
			get => diffuseColor;
			set
			{
				if( diffuseColor == value )
					return;
				diffuseColor = value;
				UpdateDynamicDiffuseScaleGpuParameter();
			}
		}

		[Category( "Diffuse" )]
		[LocalizedDisplayName( "DiffusePower", "ShaderBaseMaterial" )]
		[DefaultValue( 1.0f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 0, 10 )]
		public float DiffusePower
		{
			get => diffusePower;
			set
			{
				if( diffusePower == value )
					return;
				diffusePower = value;
				UpdateDynamicDiffuseScaleGpuParameter();
			}
		}

		[Category( "Diffuse" )]
		[LocalizedDisplayName( "DiffuseScaleDynamic", "ShaderBaseMaterial" )]
		[DefaultValue( false )]
		public bool DiffuseScaleDynamic { get; set; }

		[Category( "Diffuse" )]
		[LocalizedDisplayName( "DiffuseVertexColor", "ShaderBaseMaterial" )]
		[DefaultValue( false )]
		public bool DiffuseVertexColor { get; set; }

		[Category( "Diffuse" )]
		[LocalizedDisplayName( "Diffuse1Map", "ShaderBaseMaterial" )]
		public MapItem Diffuse1Map { get; set; }

		[Category( "Diffuse" )]
		[LocalizedDisplayName( "Diffuse2Map", "ShaderBaseMaterial" )]
		public DiffuseMapItem Diffuse2Map { get; set; }

		[Category( "Diffuse" )]
		[LocalizedDisplayName( "Diffuse3Map", "ShaderBaseMaterial" )]
		public DiffuseMapItem Diffuse3Map { get; set; }

		[Category( "Diffuse" )]
		[LocalizedDisplayName( "Diffuse4Map", "ShaderBaseMaterial" )]
		public DiffuseMapItem Diffuse4Map { get; set; }

		///////////////////////////////////////////
		//Reflection

		[Category( "Reflection Cubemap" )]
		[LocalizedDisplayName( "ReflectionColor", "ShaderBaseMaterial" )]
		[DefaultValue( typeof( ColorValue ), "0 0 0" )]
		[ColorValueNoAlphaChannel]
		public ColorValue ReflectionColor
		{
			get => reflectionColor;
			set
			{
				if( reflectionColor == value )
					return;
				reflectionColor = value;
				UpdateDynamicReflectionScaleGpuParameter();
			}
		}

		[Category( "Reflection Cubemap" )]
		[LocalizedDisplayName( "ReflectionPower", "ShaderBaseMaterial" )]
		[DefaultValue( 1.0f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 0, 10 )]
		public float ReflectionPower
		{
			get => reflectionPower;
			set
			{
				if( reflectionPower == value )
					return;
				reflectionPower = value;
				UpdateDynamicReflectionScaleGpuParameter();
			}
		}

		[Category( "Reflection Cubemap" )]
		[LocalizedDisplayName( "ReflectionScaleDynamic", "ShaderBaseMaterial" )]
		[DefaultValue( false )]
		public bool ReflectionScaleDynamic { get; set; }

		[Category( "Reflection Cubemap" )]
		[LocalizedDisplayName( "ReflectionMap", "ShaderBaseMaterial" )]
		public MapItem ReflectionMap { get; set; }

		[Category( "Reflection Cubemap" )]
		[LocalizedDisplayName( "ReflectionSpecificCubemap", "ShaderBaseMaterial" )]
		[Editor( typeof( EditorTextureUITypeEditor ), typeof( UITypeEditor ) )]
		[SupportRelativePath]
		[EditorTextureType( Texture.Type.CubeMap )]
		public string ReflectionSpecificCubemap { get; set; } = "";

		///////////////////////////////////////////
		//Emission

		[Category( "Emission" )]
		[LocalizedDisplayName( "EmissionColor", "ShaderBaseMaterial" )]
		[DefaultValue( typeof( ColorValue ), "0 0 0" )]
		[ColorValueNoAlphaChannel]
		public ColorValue EmissionColor
		{
			get => emissionColor;
			set
			{
				if( emissionColor == value )
					return;
				emissionColor = value;
				UpdateDynamicEmissionScaleGpuParameter();
			}
		}

		[Category( "Emission" )]
		[LocalizedDisplayName( "EmissionPower", "ShaderBaseMaterial" )]
		[DefaultValue( 1.0f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 0, 10 )]
		public float EmissionPower
		{
			get => emissionPower;
			set
			{
				if( emissionPower == value )
					return;
				emissionPower = value;
				UpdateDynamicEmissionScaleGpuParameter();
			}
		}

		[Category( "Emission" )]
		[LocalizedDisplayName( "EmissionScaleDynamic", "ShaderBaseMaterial" )]
		[DefaultValue( false )]
		public bool EmissionScaleDynamic { get; set; }

		[Category( "Emission" )]
		[LocalizedDisplayName( "EmissionMap", "ShaderBaseMaterial" )]
		public MapItem EmissionMap { get; set; }

		///////////////////////////////////////////
		//Specular

		[Category( "Specular" )]
		[LocalizedDisplayName( "SpecularColor", "ShaderBaseMaterial" )]
		[DefaultValue( typeof( ColorValue ), "0 0 0" )]
		[ColorValueNoAlphaChannel]
		public ColorValue SpecularColor
		{
			get => specularColor;
			set
			{
				if( specularColor == value )
					return;
				specularColor = value;
				UpdateDynamicSpecularScaleAndShininessGpuParameter();
			}
		}

		[Category( "Specular" )]
		[LocalizedDisplayName( "SpecularPower", "ShaderBaseMaterial" )]
		[DefaultValue( 1.0f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 0, 10 )]
		public float SpecularPower
		{
			get => specularPower;
			set
			{
				if( specularPower == value )
					return;
				specularPower = value;
				UpdateDynamicSpecularScaleAndShininessGpuParameter();
			}
		}

		[Category( "Specular" )]
		[LocalizedDisplayName( "SpecularScaleDynamic", "ShaderBaseMaterial" )]
		[DefaultValue( false )]
		public bool SpecularScaleDynamic { get; set; }

		[Category( "Specular" )]
		[LocalizedDisplayName( "SpecularMap", "ShaderBaseMaterial" )]
		public MapItem SpecularMap { get; set; }

		[Category( "Specular" )]
		[LocalizedDisplayName( "SpecularShininess", "ShaderBaseMaterial" )]
		[DefaultValue( 20.0f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 1, 100 )]
		public float SpecularShininess
		{
			get => specularShininess;
			set
			{
				if( specularShininess == value )
					return;
				specularShininess = value;
				UpdateDynamicSpecularScaleAndShininessGpuParameter();
			}
		}

		///////////////////////////////////////////
		//Translucency

		[Category( "Translucency" )]
		[DefaultValue( typeof( ColorValue ), "0 0 0" )]
		[ColorValueNoAlphaChannel]
		public ColorValue TranslucencyColor
		{
			get => translucencyColor;
			set
			{
				if( translucencyColor == value )
					return;
				translucencyColor = value;
				UpdateDynamicTranslucencyScaleAndClearnessGpuParameter();
			}
		}

		[Category( "Translucency" )]
		[DefaultValue( 1.0f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 0, 10 )]
		public float TranslucencyPower
		{
			get => translucencyPower;
			set
			{
				if( translucencyPower == value )
					return;
				translucencyPower = value;
				UpdateDynamicTranslucencyScaleAndClearnessGpuParameter();
			}
		}

		[Category( "Translucency" )]
		[DefaultValue( false )]
		public bool TranslucencyDynamic { get; set; }

		[Category( "Translucency" )]
		public MapItem TranslucencyMap { get; set; }

		[Category( "Translucency" )]
		[DefaultValue( 4f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 1, 256 )]
		public float TranslucencyClearness
		{
			get => translucencyClearness;
			set
			{
				if( translucencyClearness == value )
					return;
				translucencyClearness = value;
				UpdateDynamicTranslucencyScaleAndClearnessGpuParameter();
			}
		}

		///////////////////////////////////////////
		//Height

		[Category( "Height" )]
		[LocalizedDisplayName( "NormalMap", "ShaderBaseMaterial" )]
		public MapItem NormalMap { get; set; }

		[Category( "Height" )]
		[LocalizedDisplayName( "HeightMap", "ShaderBaseMaterial" )]
		public MapItem HeightMap { get; set; }

		[Category( "Height" )]
		[LocalizedDisplayName( "HeightFromNormalMapAlpha", "ShaderBaseMaterial" )]
		[DefaultValue( false )]
		public bool HeightFromNormalMapAlpha { get; set; }

		[Category( "Height" )]
		[LocalizedDisplayName( "DisplacementTechnique", "ShaderBaseMaterial" )]
		[DefaultValue( DisplacementTechniques.ParallaxOcclusionMapping )]
		public DisplacementTechniques DisplacementTechnique { get; set; } = DisplacementTechniques.ParallaxOcclusionMapping;

		[Category( "Height" )]
		[LocalizedDisplayName( "HeightScale", "ShaderBaseMaterial" )]
		[DefaultValue( .04f )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( .01f, .1f )]
		public float HeightScale
		{
			get => heightScale;
			set
			{
				if( heightScale == value )
					return;
				heightScale = value;
				UpdateHeightScaleGpuParameter();
			}
		}

		///////////////////////////////////////////

		public ShaderBaseMaterial()
		{
			Diffuse1Map = new MapItem( this );
			Diffuse2Map = new DiffuseMapItem( this );
			Diffuse3Map = new DiffuseMapItem( this );
			Diffuse4Map = new DiffuseMapItem( this );
			ReflectionMap = new MapItem( this );
			EmissionMap = new MapItem( this );
			SpecularMap = new MapItem( this );
			TranslucencyMap = new MapItem( this );
			NormalMap = new MapItem( this );
			HeightMap = new MapItem( this );
		}

		public override void Dispose()
		{
			base.Dispose();
		}

		protected override void OnClone( HighLevelMaterial sourceMaterial )
		{
			base.OnClone( sourceMaterial );

			var source = (ShaderBaseMaterial)sourceMaterial;

			//General
			Blending = source.Blending;
			Lighting = source.Lighting;
			AmbientLighting = source.AmbientLighting;
			DoubleSided = source.DoubleSided;
			UseNormals = source.UseNormals;
			ReceiveShadows = source.ReceiveShadows;
			ReceiveSimpleShadows = source.ReceiveSimpleShadows;
			AlphaRejectFunction = source.AlphaRejectFunction;
			AlphaRejectValue = source.AlphaRejectValue;
			AlphaToCoverage = source.AlphaToCoverage;
			fadingByDistanceRange = source.fadingByDistanceRange;
			AllowFog = source.AllowFog;
			DepthWrite = source.DepthWrite;
			DepthTest = source.DepthTest;
			SoftParticles = source.SoftParticles;
			softParticlesFadingLength = source.softParticlesFadingLength;
			depthOffset = source.depthOffset;
			HalfLambert = source.HalfLambert;

			//Diffuse
			diffuseColor = source.diffuseColor;
			diffusePower = source.diffusePower;
			DiffuseScaleDynamic = source.DiffuseScaleDynamic;
			DiffuseVertexColor = source.DiffuseVertexColor;
			Diffuse1Map.OnClone( source.Diffuse1Map );
			Diffuse2Map.OnClone( source.Diffuse2Map );
			Diffuse3Map.OnClone( source.Diffuse3Map );
			Diffuse4Map.OnClone( source.Diffuse4Map );

			//Reflection
			reflectionColor = source.reflectionColor;
			reflectionPower = source.reflectionPower;
			ReflectionScaleDynamic = source.ReflectionScaleDynamic;
			ReflectionMap.OnClone( source.ReflectionMap );
			ReflectionSpecificCubemap = source.ConvertToFullPath( source.ReflectionSpecificCubemap );

			//Emission
			emissionColor = source.emissionColor;
			emissionPower = source.emissionPower;
			EmissionScaleDynamic = source.EmissionScaleDynamic;
			EmissionMap.OnClone( source.EmissionMap );

			//Specular
			specularColor = source.specularColor;
			specularPower = source.specularPower;
			SpecularScaleDynamic = source.SpecularScaleDynamic;
			SpecularMap.OnClone( source.SpecularMap );
			specularShininess = source.specularShininess;

			//Translucency
			translucencyColor = source.translucencyColor;
			translucencyPower = source.translucencyPower;
			TranslucencyDynamic = source.TranslucencyDynamic;
			TranslucencyMap.OnClone( source.TranslucencyMap );
			translucencyClearness = source.translucencyClearness;

			//Height
			NormalMap.OnClone( source.NormalMap );
			HeightMap.OnClone( source.HeightMap );
			HeightFromNormalMapAlpha = source.HeightFromNormalMapAlpha;
			DisplacementTechnique = source.DisplacementTechnique;
			heightScale = source.heightScale;
		}

		protected override bool OnLoad( TextBlock block )
		{
			if( !base.OnLoad( block ) )
				return false;

			//General
			{
				if( block.IsAttributeExist( "blending" ) )
					Blending = (MaterialBlendingTypes)Enum.Parse(
						typeof( MaterialBlendingTypes ), block.GetAttribute( "blending" ) );

				if( block.IsAttributeExist( "lighting" ) )
					Lighting = bool.Parse( block.GetAttribute( "lighting" ) );

				if( block.IsAttributeExist( "ambientLighting" ) )
					AmbientLighting = bool.Parse( block.GetAttribute( "ambientLighting" ) );

				if( block.IsAttributeExist( "doubleSided" ) )
					DoubleSided = bool.Parse( block.GetAttribute( "doubleSided" ) );
				//old version compatibility
				if( block.IsAttributeExist( "culling" ) )
					DoubleSided = !bool.Parse( block.GetAttribute( "culling" ) );

				if( block.IsAttributeExist( "useNormals" ) )
					UseNormals = bool.Parse( block.GetAttribute( "useNormals" ) );

				if( block.IsAttributeExist( "receiveShadows" ) )
					ReceiveShadows = bool.Parse( block.GetAttribute( "receiveShadows" ) );

				if( block.IsAttributeExist( "receiveSimpleShadows" ) )
					ReceiveSimpleShadows = bool.Parse( block.GetAttribute( "receiveSimpleShadows" ) );

				if( block.IsAttributeExist( "alphaRejectFunction" ) )
					AlphaRejectFunction = (CompareFunction)Enum.Parse( typeof( CompareFunction ),
						block.GetAttribute( "alphaRejectFunction" ) );

				if( block.IsAttributeExist( "alphaRejectValue" ) )
					AlphaRejectValue = byte.Parse( block.GetAttribute( "alphaRejectValue" ) );

				if( block.IsAttributeExist( "alphaToCoverage" ) )
					AlphaToCoverage = bool.Parse( block.GetAttribute( "alphaToCoverage" ) );

				if( block.IsAttributeExist( "fadingByDistanceRange" ) )
					fadingByDistanceRange = Range.Parse( block.GetAttribute( "fadingByDistanceRange" ) );

				if( block.IsAttributeExist( "allowFog" ) )
					AllowFog = bool.Parse( block.GetAttribute( "allowFog" ) );

				if( block.IsAttributeExist( "depthWrite" ) )
					DepthWrite = bool.Parse( block.GetAttribute( "depthWrite" ) );

				if( block.IsAttributeExist( "depthTest" ) )
					DepthTest = bool.Parse( block.GetAttribute( "depthTest" ) );

				if( block.IsAttributeExist( "softParticles" ) )
					SoftParticles = bool.Parse( block.GetAttribute( "softParticles" ) );

				if( block.IsAttributeExist( "softParticlesFadingLength" ) )
					softParticlesFadingLength = float.Parse( block.GetAttribute( "softParticlesFadingLength" ) );

				if( block.IsAttributeExist( "depthOffset" ) )
					depthOffset = float.Parse( block.GetAttribute( "depthOffset" ) );

				if( block.IsAttributeExist( "halfLambert" ) )
					HalfLambert = bool.Parse( block.GetAttribute( "halfLambert" ) );
			}

			//Diffuse
			{
				//old version compatibility
				if( block.IsAttributeExist( "diffuseScale" ) )
				{
					diffuseColor = ColorValue.Parse( block.GetAttribute( "diffuseScale" ) );
					var power = Math.Max( Math.Max( diffuseColor.Red, diffuseColor.Green ),
						diffuseColor.Blue );
					if( power > 1 )
					{
						diffuseColor.Red /= power;
						diffuseColor.Green /= power;
						diffuseColor.Blue /= power;
						diffusePower = power;
					}
				}

				if( block.IsAttributeExist( "diffuseColor" ) )
					diffuseColor = ColorValue.Parse( block.GetAttribute( "diffuseColor" ) );
				if( block.IsAttributeExist( "diffusePower" ) )
					diffusePower = float.Parse( block.GetAttribute( "diffusePower" ) );

				if( block.IsAttributeExist( "diffuseScaleDynamic" ) )
					DiffuseScaleDynamic = bool.Parse( block.GetAttribute( "diffuseScaleDynamic" ) );

				if( block.IsAttributeExist( "diffuseVertexColor" ) )
					DiffuseVertexColor = bool.Parse( block.GetAttribute( "diffuseVertexColor" ) );

				var diffuse1MapBlock = block.FindChild( "diffuse1Map" );
				if( diffuse1MapBlock != null )
					Diffuse1Map.Load( diffuse1MapBlock );

				var diffuse2MapBlock = block.FindChild( "diffuse2Map" );
				if( diffuse2MapBlock != null )
					Diffuse2Map.Load( diffuse2MapBlock );

				var diffuse3MapBlock = block.FindChild( "diffuse3Map" );
				if( diffuse3MapBlock != null )
					Diffuse3Map.Load( diffuse3MapBlock );

				var diffuse4MapBlock = block.FindChild( "diffuse4Map" );
				if( diffuse4MapBlock != null )
					Diffuse4Map.Load( diffuse4MapBlock );

				//old version compatibility
				if( block.IsAttributeExist( "diffuseMap" ) )
					Diffuse1Map.Texture = block.GetAttribute( "diffuseMap" );
			}

			//Reflection
			{
				if( block.IsAttributeExist( "reflectionScale" ) )
				{
					reflectionColor = ColorValue.Parse( block.GetAttribute( "reflectionScale" ) );
					var power = Math.Max( Math.Max( reflectionColor.Red, reflectionColor.Green ),
						Math.Max( reflectionColor.Blue, reflectionColor.Alpha ) );
					if( power > 1 )
					{
						reflectionColor /= power;
						reflectionPower = power;
					}
				}

				if( block.IsAttributeExist( "reflectionColor" ) )
					reflectionColor = ColorValue.Parse( block.GetAttribute( "reflectionColor" ) );
				if( block.IsAttributeExist( "reflectionPower" ) )
					reflectionPower = float.Parse( block.GetAttribute( "reflectionPower" ) );

				if( block.IsAttributeExist( "reflectionScaleDynamic" ) )
					ReflectionScaleDynamic = bool.Parse( block.GetAttribute( "reflectionScaleDynamic" ) );

				var reflectionMapBlock = block.FindChild( "reflectionMap" );
				if( reflectionMapBlock != null )
					ReflectionMap.Load( reflectionMapBlock );

				if( block.IsAttributeExist( "reflectionSpecificCubemap" ) )
					ReflectionSpecificCubemap = block.GetAttribute( "reflectionSpecificCubemap" );

				//old version compatibility
				if( block.IsAttributeExist( "reflectionMap" ) )
					ReflectionMap.Texture = block.GetAttribute( "reflectionMap" );
			}

			//Emission
			{
				if( block.IsAttributeExist( "emissionScale" ) )
				{
					emissionColor = ColorValue.Parse( block.GetAttribute( "emissionScale" ) );
					var power = Math.Max( Math.Max( emissionColor.Red, emissionColor.Green ),
						Math.Max( emissionColor.Blue, emissionColor.Alpha ) );
					if( power > 1 )
					{
						emissionColor /= power;
						emissionPower = power;
					}
				}

				if( block.IsAttributeExist( "emissionColor" ) )
					emissionColor = ColorValue.Parse( block.GetAttribute( "emissionColor" ) );
				if( block.IsAttributeExist( "emissionPower" ) )
					emissionPower = float.Parse( block.GetAttribute( "emissionPower" ) );

				if( block.IsAttributeExist( "emissionScaleDynamic" ) )
					EmissionScaleDynamic = bool.Parse( block.GetAttribute( "emissionScaleDynamic" ) );

				var emissionMapBlock = block.FindChild( "emissionMap" );
				if( emissionMapBlock != null )
					EmissionMap.Load( emissionMapBlock );

				//old version compatibility
				if( block.IsAttributeExist( "emissionMap" ) )
					EmissionMap.Texture = block.GetAttribute( "emissionMap" );
			}

			//Specular
			{
				if( block.IsAttributeExist( "specularScale" ) )
				{
					specularColor = ColorValue.Parse( block.GetAttribute( "specularScale" ) );
					var power = Math.Max( Math.Max( specularColor.Red, specularColor.Green ),
						Math.Max( specularColor.Blue, specularColor.Alpha ) );
					if( power > 1 )
					{
						specularColor /= power;
						specularPower = power;
					}
				}

				if( block.IsAttributeExist( "specularColor" ) )
					specularColor = ColorValue.Parse( block.GetAttribute( "specularColor" ) );
				if( block.IsAttributeExist( "specularPower" ) )
					specularPower = float.Parse( block.GetAttribute( "specularPower" ) );

				if( block.IsAttributeExist( "specularScaleDynamic" ) )
					SpecularScaleDynamic = bool.Parse( block.GetAttribute( "specularScaleDynamic" ) );

				var specularMapBlock = block.FindChild( "specularMap" );
				if( specularMapBlock != null )
					SpecularMap.Load( specularMapBlock );

				if( block.IsAttributeExist( "specularShininess" ) )
					specularShininess = float.Parse( block.GetAttribute( "specularShininess" ) );

				//old version compatibility
				if( block.IsAttributeExist( "specularMap" ) )
					SpecularMap.Texture = block.GetAttribute( "specularMap" );
			}

			//Translucency
			{
				if( block.IsAttributeExist( "translucencyColor" ) )
					translucencyColor = ColorValue.Parse( block.GetAttribute( "translucencyColor" ) );
				if( block.IsAttributeExist( "translucencyPower" ) )
					translucencyPower = float.Parse( block.GetAttribute( "translucencyPower" ) );

				if( block.IsAttributeExist( "translucencyDynamic" ) )
					TranslucencyDynamic = bool.Parse( block.GetAttribute( "translucencyDynamic" ) );

				var translucencyMapBlock = block.FindChild( "translucencyMap" );
				if( translucencyMapBlock != null )
					TranslucencyMap.Load( translucencyMapBlock );

				if( block.IsAttributeExist( "translucencyClearness" ) )
					translucencyClearness = float.Parse( block.GetAttribute( "translucencyClearness" ) );
			}

			//Height
			{
				var normalMapBlock = block.FindChild( "normalMap" );
				if( normalMapBlock != null )
					NormalMap.Load( normalMapBlock );

				var heightMapBlock = block.FindChild( "heightMap" );
				if( heightMapBlock != null )
					HeightMap.Load( heightMapBlock );

				if( block.IsAttributeExist( "heightFromNormalMapAlpha" ) )
					HeightFromNormalMapAlpha = bool.Parse( block.GetAttribute( "heightFromNormalMapAlpha" ) );

				if( block.IsAttributeExist( "displacementTechnique" ) )
				{
					DisplacementTechnique = (DisplacementTechniques)Enum.Parse( typeof( DisplacementTechniques ),
						block.GetAttribute( "displacementTechnique" ) );
				}

				if( block.IsAttributeExist( "heightScale" ) )
					heightScale = float.Parse( block.GetAttribute( "heightScale" ) );

				//old version compatibility
				if( block.IsAttributeExist( "normalMap" ) )
					NormalMap.Texture = block.GetAttribute( "normalMap" );
				if( block.IsAttributeExist( "heightMap" ) )
					HeightMap.Texture = block.GetAttribute( "heightMap" );
			}

			return true;
		}

		protected override void OnSave( TextBlock block )
		{
			base.OnSave( block );

			//General
			{
				if( Blending != MaterialBlendingTypes.Opaque )
					block.SetAttribute( "blending", Blending.ToString() );

				if( !Lighting )
					block.SetAttribute( "lighting", Lighting.ToString() );

				if( !AmbientLighting )
					block.SetAttribute( "ambientLighting", AmbientLighting.ToString() );

				if( DoubleSided )
					block.SetAttribute( "doubleSided", DoubleSided.ToString() );

				if( !UseNormals )
					block.SetAttribute( "useNormals", UseNormals.ToString() );

				if( !ReceiveShadows )
					block.SetAttribute( "receiveShadows", ReceiveShadows.ToString() );

				if( ReceiveSimpleShadows )
					block.SetAttribute( "receiveSimpleShadows", ReceiveSimpleShadows.ToString() );

				if( AlphaRejectFunction != CompareFunction.AlwaysPass )
					block.SetAttribute( "alphaRejectFunction", AlphaRejectFunction.ToString() );

				if( AlphaRejectValue != 127 )
					block.SetAttribute( "alphaRejectValue", AlphaRejectValue.ToString() );

				if( AlphaToCoverage )
					block.SetAttribute( "alphaToCoverage", AlphaToCoverage.ToString() );

				if( fadingByDistanceRange != new Range( 0, 0 ) )
					block.SetAttribute( "fadingByDistanceRange", fadingByDistanceRange.ToString() );

				if( !AllowFog )
					block.SetAttribute( "allowFog", AllowFog.ToString() );

				if( !DepthWrite )
					block.SetAttribute( "depthWrite", DepthWrite.ToString() );

				if( !DepthTest )
					block.SetAttribute( "depthTest", DepthTest.ToString() );

				if( SoftParticles )
					block.SetAttribute( "softParticles", SoftParticles.ToString() );

				if( softParticlesFadingLength != 1 )
					block.SetAttribute( "softParticlesFadingLength", softParticlesFadingLength.ToString() );

				if( depthOffset != 0 )
					block.SetAttribute( "depthOffset", depthOffset.ToString() );

				if( HalfLambert )
					block.SetAttribute( "halfLambert", HalfLambert.ToString() );
			}

			//Diffuse
			{
				if( diffuseColor != new ColorValue( 1, 1, 1 ) )
					block.SetAttribute( "diffuseColor", diffuseColor.ToString() );
				if( diffusePower != 1 )
					block.SetAttribute( "diffusePower", diffusePower.ToString() );

				if( DiffuseScaleDynamic )
					block.SetAttribute( "diffuseScaleDynamic", DiffuseScaleDynamic.ToString() );

				if( DiffuseVertexColor )
					block.SetAttribute( "diffuseVertexColor", DiffuseVertexColor.ToString() );

				if( Diffuse1Map.IsDataExists() )
				{
					var diffuse1MapBlock = block.AddChild( "diffuse1Map" );
					Diffuse1Map.Save( diffuse1MapBlock );
				}

				if( Diffuse2Map.IsDataExists() )
				{
					var diffuse2MapBlock = block.AddChild( "diffuse2Map" );
					Diffuse2Map.Save( diffuse2MapBlock );
				}

				if( Diffuse3Map.IsDataExists() )
				{
					var diffuse3MapBlock = block.AddChild( "diffuse3Map" );
					Diffuse3Map.Save( diffuse3MapBlock );
				}

				if( Diffuse4Map.IsDataExists() )
				{
					var diffuse4MapBlock = block.AddChild( "diffuse4Map" );
					Diffuse4Map.Save( diffuse4MapBlock );
				}
			}

			//Reflection
			{
				if( reflectionColor != new ColorValue( 0, 0, 0 ) )
					block.SetAttribute( "reflectionColor", reflectionColor.ToString() );
				if( reflectionPower != 1 )
					block.SetAttribute( "reflectionPower", reflectionPower.ToString() );

				if( ReflectionScaleDynamic )
					block.SetAttribute( "reflectionScaleDynamic", ReflectionScaleDynamic.ToString() );

				if( ReflectionMap.IsDataExists() )
				{
					var reflectionMapBlock = block.AddChild( "reflectionMap" );
					ReflectionMap.Save( reflectionMapBlock );
				}

				if( !string.IsNullOrEmpty( ReflectionSpecificCubemap ) )
					block.SetAttribute( "reflectionSpecificCubemap", ReflectionSpecificCubemap );
			}

			//Emission
			{
				if( emissionColor != new ColorValue( 0, 0, 0 ) )
					block.SetAttribute( "emissionColor", emissionColor.ToString() );
				if( emissionPower != 1 )
					block.SetAttribute( "emissionPower", emissionPower.ToString() );

				if( EmissionScaleDynamic )
					block.SetAttribute( "emissionScaleDynamic", EmissionScaleDynamic.ToString() );

				if( EmissionMap.IsDataExists() )
				{
					var emissionMapBlock = block.AddChild( "emissionMap" );
					EmissionMap.Save( emissionMapBlock );
				}
			}

			//Specular
			{
				if( specularColor != new ColorValue( 0, 0, 0 ) )
					block.SetAttribute( "specularColor", specularColor.ToString() );
				if( specularPower != 1 )
					block.SetAttribute( "specularPower", specularPower.ToString() );

				if( SpecularScaleDynamic )
					block.SetAttribute( "specularScaleDynamic", SpecularScaleDynamic.ToString() );

				if( SpecularMap.IsDataExists() )
				{
					var specularMapBlock = block.AddChild( "specularMap" );
					SpecularMap.Save( specularMapBlock );
				}

				if( specularShininess != 20 )
					block.SetAttribute( "specularShininess", specularShininess.ToString() );
			}

			//Translucency
			{
				if( translucencyColor != new ColorValue( 0, 0, 0 ) )
					block.SetAttribute( "translucencyColor", translucencyColor.ToString() );
				if( translucencyPower != 1 )
					block.SetAttribute( "translucencyPower", translucencyPower.ToString() );

				if( TranslucencyDynamic )
					block.SetAttribute( "translucencyDynamic", TranslucencyDynamic.ToString() );

				if( TranslucencyMap.IsDataExists() )
				{
					var translucencyMapBlock = block.AddChild( "translucencyMap" );
					TranslucencyMap.Save( translucencyMapBlock );
				}

				if( translucencyClearness != 4f )
					block.SetAttribute( "translucencyClearness", translucencyClearness.ToString() );
			}

			//Height
			{
				if( NormalMap.IsDataExists() )
				{
					var normalMapBlock = block.AddChild( "normalMap" );
					NormalMap.Save( normalMapBlock );
				}

				if( HeightFromNormalMapAlpha )
					block.SetAttribute( "heightFromNormalMapAlpha", HeightFromNormalMapAlpha.ToString() );

				if( HeightMap.IsDataExists() )
				{
					var heightMapBlock = block.AddChild( "heightMap" );
					HeightMap.Save( heightMapBlock );
				}

				if( DisplacementTechnique != DisplacementTechniques.ParallaxOcclusionMapping )
					block.SetAttribute( "displacementTechnique", DisplacementTechnique.ToString() );

				if( heightScale != .04f )
					block.SetAttribute( "heightScale", heightScale.ToString() );
			}
		}

		void SetProgramAutoConstants_Main_Vertex( GpuProgramParameters parameters, int lightCount )
		{
			var shadowMap = SceneManager.Instance.IsShadowTechniqueShadowmapBased() && ReceiveShadows &&
				lightCount != 0;

			parameters.SetNamedAutoConstant( "worldMatrix",
				GpuProgramParameters.AutoConstantType.WorldMatrix );
			parameters.SetNamedAutoConstant( "viewProjMatrix",
				GpuProgramParameters.AutoConstantType.ViewProjMatrix );
			parameters.SetNamedAutoConstant( "cameraPositionObjectSpace",
				GpuProgramParameters.AutoConstantType.CameraPositionObjectSpace );
			parameters.SetNamedAutoConstant( "cameraPosition",
				GpuProgramParameters.AutoConstantType.CameraPosition );

			if( lightCount != 0 )
			{
				if( shadowMap )
				{
					parameters.SetNamedAutoConstant( "textureViewProjMatrix0",
					GpuProgramParameters.AutoConstantType.TextureViewProjMatrix, 0 );
					parameters.SetNamedAutoConstant( "textureViewProjMatrix1",
						GpuProgramParameters.AutoConstantType.TextureViewProjMatrix, 1 );
					parameters.SetNamedAutoConstant( "textureViewProjMatrix2",
						GpuProgramParameters.AutoConstantType.TextureViewProjMatrix, 2 );
					parameters.SetNamedAutoConstant( "shadowFarDistance",
						GpuProgramParameters.AutoConstantType.ShadowFarDistance );
					parameters.SetNamedAutoConstant( "shadowTextureSizes",
						GpuProgramParameters.AutoConstantType.ShadowTextureSizes );
					if( SceneManager.Instance.IsShadowTechniquePSSM() )
					{
						parameters.SetNamedAutoConstant( "shadowDirectionalLightSplitDistances",
							GpuProgramParameters.AutoConstantType.ShadowDirectionalLightSplitDistances );
					}
				}

				parameters.SetNamedAutoConstant( "lightPositionArray",
					GpuProgramParameters.AutoConstantType.LightPositionArray, lightCount );
				parameters.SetNamedAutoConstant( "lightPositionObjectSpaceArray",
					GpuProgramParameters.AutoConstantType.LightPositionObjectSpaceArray, lightCount );
				parameters.SetNamedAutoConstant( "lightDirectionArray",
					GpuProgramParameters.AutoConstantType.LightDirectionArray, lightCount );
				parameters.SetNamedAutoConstant( "lightAttenuationArray",
					GpuProgramParameters.AutoConstantType.LightAttenuationArray, lightCount );
				parameters.SetNamedAutoConstant( "spotLightParamsArray",
					GpuProgramParameters.AutoConstantType.SpotLightParamsArray, lightCount );
				parameters.SetNamedAutoConstant( "lightCustomShaderParameterArray",
					GpuProgramParameters.AutoConstantType.LightCustomShaderParameterArray, lightCount );
			}

			//instancing
			if( RenderSystem.Instance.HasShaderModel3() &&
				RenderSystem.Instance.Capabilities.HardwareInstancing )
			{
				parameters.SetNamedAutoConstant( "instancing", GpuProgramParameters.AutoConstantType.Instancing );
			}

			//1 hour interval for better precision.
			parameters.SetNamedAutoConstantFloat( "time",
				GpuProgramParameters.AutoConstantType.Time0X, 3600.0f );
		}

		void SetProgramAutoConstants_Main_Fragment( GpuProgramParameters parameters, int lightCount )
		{
			var shadowMap = SceneManager.Instance.IsShadowTechniqueShadowmapBased() && ReceiveShadows &&
				lightCount != 0;

			parameters.SetNamedAutoConstant( "farClipDistance",
				GpuProgramParameters.AutoConstantType.FarClipDistance );

			if( shadowMap )
			{
				parameters.SetNamedAutoConstant( "drawShadowDebugging",
					GpuProgramParameters.AutoConstantType.DrawShadowDebugging );
			}

			//viewportSize
			if( SoftParticles )
			{
				parameters.SetNamedAutoConstant( "viewportSize",
					GpuProgramParameters.AutoConstantType.ViewportSize );
			}

			//Light
			parameters.SetNamedAutoConstant( "ambientLightColor",
				GpuProgramParameters.AutoConstantType.AmbientLightColor );

			if( lightCount != 0 )
			{
				if( shadowMap )
				{
					parameters.SetNamedAutoConstant( "lightShadowFarClipDistance",
						GpuProgramParameters.AutoConstantType.LightShadowFarClipDistance, 0 );
					parameters.SetNamedAutoConstant( "shadowFarDistance",
						GpuProgramParameters.AutoConstantType.ShadowFarDistance );
					parameters.SetNamedAutoConstant( "shadowColorIntensity",
						GpuProgramParameters.AutoConstantType.ShadowColorIntensity );
					parameters.SetNamedAutoConstant( "shadowTextureSizes",
						GpuProgramParameters.AutoConstantType.ShadowTextureSizes );
					if( SceneManager.Instance.IsShadowTechniquePSSM() )
					{
						parameters.SetNamedAutoConstant( "shadowDirectionalLightSplitDistances",
							GpuProgramParameters.AutoConstantType.ShadowDirectionalLightSplitDistances );
					}
				}

				parameters.SetNamedAutoConstant( "lightPositionArray",
					GpuProgramParameters.AutoConstantType.LightPositionArray, lightCount );
				parameters.SetNamedAutoConstant( "lightDirectionArray",
					GpuProgramParameters.AutoConstantType.LightDirectionArray, lightCount );
				parameters.SetNamedAutoConstant( "lightAttenuationArray",
					GpuProgramParameters.AutoConstantType.LightAttenuationArray, lightCount );
				parameters.SetNamedAutoConstant( "lightDiffuseColorPowerScaledArray",
					GpuProgramParameters.AutoConstantType.LightDiffuseColorPowerScaledArray, lightCount );
				parameters.SetNamedAutoConstant( "lightSpecularColorPowerScaledArray",
					GpuProgramParameters.AutoConstantType.LightSpecularColorPowerScaledArray, lightCount );
				parameters.SetNamedAutoConstant( "spotLightParamsArray",
					GpuProgramParameters.AutoConstantType.SpotLightParamsArray, lightCount );
				parameters.SetNamedAutoConstant( "lightCastShadowsArray",
					GpuProgramParameters.AutoConstantType.LightCastShadowsArray, lightCount );
				parameters.SetNamedAutoConstant( "lightCustomShaderParameterArray",
					GpuProgramParameters.AutoConstantType.LightCustomShaderParameterArray, lightCount );
			}

			//Fog
			if( AllowFog && SceneManager.Instance.GetFogMode() != FogMode.None )
			{
				parameters.SetNamedAutoConstant( "fogParams",
					GpuProgramParameters.AutoConstantType.FogParams );
				parameters.SetNamedAutoConstant( "fogColor",
					GpuProgramParameters.AutoConstantType.FogColor );
			}

			//lightmap
			if( LightmapTexCoordIndex != -1 )
			{
				parameters.SetNamedAutoConstant( "lightmapUVTransform",
					GpuProgramParameters.AutoConstantType.LightmapUVTransform );
			}

			//clip planes
			if( RenderSystem.Instance.IsOpenGL() )
			{
				for( var n = 0; n < 6; n++ )
				{
					parameters.SetNamedAutoConstant( "clipPlane" + n.ToString(),
						GpuProgramParameters.AutoConstantType.ClipPlane, n );
				}
			}

			if( RenderSystem.Instance.IsOpenGLES() )
			{
				parameters.SetNamedAutoConstant( "alphaRejectValue",
					GpuProgramParameters.AutoConstantType.AlphaRejectValue );
			}

			//1 hour interval for better precision.
			parameters.SetNamedAutoConstantFloat( "time",
				GpuProgramParameters.AutoConstantType.Time0X, 3600.0f );
		}

		void SetProgramAutoConstants_ShadowCaster_Vertex( GpuProgramParameters parameters )
		{
			parameters.SetNamedAutoConstant( "worldMatrix",
				GpuProgramParameters.AutoConstantType.WorldMatrix );
			parameters.SetNamedAutoConstant( "viewProjMatrix",
				GpuProgramParameters.AutoConstantType.ViewProjMatrix );
			parameters.SetNamedAutoConstant( "cameraPosition",
				GpuProgramParameters.AutoConstantType.CameraPosition );
			parameters.SetNamedAutoConstant( "texelOffsets",
				GpuProgramParameters.AutoConstantType.TexelOffsets );

			if( RenderSystem.Instance.HasShaderModel3() &&
				RenderSystem.Instance.Capabilities.HardwareInstancing )
			{
				parameters.SetNamedAutoConstant( "instancing", GpuProgramParameters.AutoConstantType.Instancing );
			}

			//1 hour interval for better precision.
			parameters.SetNamedAutoConstantFloat( "time",
				GpuProgramParameters.AutoConstantType.Time0X, 3600.0f );
		}

		void SetProgramAutoConstants_ShadowCaster_Fragment( GpuProgramParameters parameters )
		{
			parameters.SetNamedAutoConstant( "farClipDistance",
				GpuProgramParameters.AutoConstantType.FarClipDistance );
			parameters.SetNamedAutoConstant( "shadowDirectionalLightBias",
				GpuProgramParameters.AutoConstantType.ShadowDirectionalLightBias );
			parameters.SetNamedAutoConstant( "shadowSpotLightBias",
				GpuProgramParameters.AutoConstantType.ShadowSpotLightBias );
			parameters.SetNamedAutoConstant( "shadowPointLightBias",
				GpuProgramParameters.AutoConstantType.ShadowPointLightBias );

			parameters.SetNamedAutoConstant( "alphaRejectValue",
				GpuProgramParameters.AutoConstantType.AlphaRejectValue );

			//1 hour interval for better precision.
			parameters.SetNamedAutoConstantFloat( "time",
				GpuProgramParameters.AutoConstantType.Time0X, 3600.0f );
		}

		protected virtual void OnSetProgramAutoConstants( GpuProgramParameters parameters, int lightCount,
			GpuProgramType programType, bool shadowCasterPass )
		{
			if( shadowCasterPass )
			{
				if( programType == GpuProgramType.Vertex )
					SetProgramAutoConstants_ShadowCaster_Vertex( parameters );
				else
					SetProgramAutoConstants_ShadowCaster_Fragment( parameters );
			}
			else
			{
				if( programType == GpuProgramType.Vertex )
					SetProgramAutoConstants_Main_Vertex( parameters, lightCount );
				else
					SetProgramAutoConstants_Main_Fragment( parameters, lightCount );
			}
		}

		protected virtual string OnGetExtensionFileName()
		{
			return null;
		}

		void GenerateTexCoordString( StringBuilder builder, int texCoord, TransformItem transformItem,
			string transformGpuParameterNamePrefix )
		{
			if( transformItem.IsDataExists() )
			{
				builder.AppendFormat(
					"mul(float2x2({0}Mul.x,{0}Mul.y,{0}Mul.z,{0}Mul.w),texCoord{1})+{0}Add",
					transformGpuParameterNamePrefix, texCoord );
			}
			else
			{
				builder.AppendFormat( "texCoord{0}", texCoord );
			}
		}

		protected virtual bool OnIsNeedSpecialShadowCasterMaterial()
		{
			if( AlphaRejectFunction != CompareFunction.AlwaysPass )
				return true;
			return false;
		}

		protected virtual void OnAddCompileArguments( StringBuilder arguments ) { }

		bool CreateDefaultTechnique( out bool shadersIsNotSupported )
		{
			var loadTextures = true;
			if( EngineApp.Instance.ApplicationType == EngineApp.ApplicationTypes.ShaderCacheCompiler )
				loadTextures = false;

			shadersIsNotSupported = false;

			const string sourceFileMain = "Base\\Shaders\\ShaderBase_main.cg_hlsl";
			const string sourceFileShadowCaster = "Base\\Shaders\\ShaderBase_shadowCaster.cg_hlsl";

			string vertexSyntax;
			string fragmentSyntax;
			if( RenderSystem.Instance.IsDirect3D() )
			{
				vertexSyntax = "vs_3_0";
				fragmentSyntax = "ps_3_0";
			}
			else if( RenderSystem.Instance.IsOpenGLES() )
			{
				vertexSyntax = "hlsl2glsl";
				fragmentSyntax = "hlsl2glsl";
			}
			else
			{
				vertexSyntax = "arbvp1";
				fragmentSyntax = "arbfp1";
			}

			//technique is supported?
			{
				if( !GpuProgramManager.Instance.IsSyntaxSupported( fragmentSyntax ) )
				{
					defaultTechniqueErrorString = $"The fragment shaders ({fragmentSyntax}) are not supported.";
					shadersIsNotSupported = true;
					return false;
				}

				if( !GpuProgramManager.Instance.IsSyntaxSupported( vertexSyntax ) )
				{
					defaultTechniqueErrorString = $"The vertex shaders ({vertexSyntax}) are not supported.";
					shadersIsNotSupported = true;
					return false;
				}
			}

			var maxSamplerCount = 16;
			int maxTexCoordCount;
			if( RenderSystem.Instance.IsDirect3D() || RenderSystem.Instance.IsOpenGL() )
				maxTexCoordCount = 10;
			else
				maxTexCoordCount = 8;

			var supportAtiHardwareShadows = false;
			var supportNvidiaHardwareShadows = false;
			{
				if( ( RenderSystem.Instance.IsDirect3D() || RenderSystem.Instance.IsOpenGL() ) &&
					SceneManager.Instance.IsShadowTechniqueShadowmapBased() && RenderSystem.Instance.HasShaderModel3() &&
					TextureManager.Instance.IsFormatSupported( Texture.Type.Type2D, PixelFormat.Depth24, Texture.Usage.RenderTarget ) )
				{
					if( RenderSystem.Instance.Capabilities.Vendor == GPUVendors.ATI )
						supportAtiHardwareShadows = true;
					if( RenderSystem.Instance.Capabilities.Vendor == GPUVendors.NVidia )
						supportNvidiaHardwareShadows = true;
				}
			}


			BaseMaterial.ReceiveShadows = ReceiveShadows;

			var doubleSidedTwoPassMode = false;
			if( DoubleSided )
			{
				if( RenderSystem.Instance.IsOpenGL() && !RenderSystem.Instance.HasShaderModel3() )
					doubleSidedTwoPassMode = true;
				if( RenderSystem.Instance.IsOpenGLES() )
					doubleSidedTwoPassMode = true;
			}

			//create techniques
			foreach( MaterialSchemes materialScheme in Enum.GetValues( typeof( MaterialSchemes ) ) )
			{
				var technique = BaseMaterial.CreateTechnique();
				technique.SchemeName = materialScheme.ToString();

				//for Shader Model 2, for stencil shadows, for not opaque blending.
				//pass 0: ambient pass (optional)
				//pass 1: directional light
				//pass 2: point light
				//pass 3: spot light

				//for Shader Model 3
				//pass 0: ambient pass
				//pass 1: ambient pass + first directional light
				//pass 2: directional light (ignore first directional light)
				//pass 3: point light
				//pass 4: spot light

				var mergeAmbientAndDirectionalLightPasses = RenderSystem.Instance.HasShaderModel3() &&
					Blending == MaterialBlendingTypes.Opaque &&
					!SceneManager.Instance.IsShadowTechniqueStencilBased();

				var needAmbientPass = AmbientLighting || emissionColor != new ColorValue( 0, 0, 0 ) ||
					EmissionScaleDynamic || Blending == MaterialBlendingTypes.Opaque;

				int passCount;
				if( Lighting )
				{
					passCount = mergeAmbientAndDirectionalLightPasses ? 4 : 3;
					if( needAmbientPass )
						passCount++;
				}
				else
					passCount = 1;

				for( var nPass = 0; nPass < passCount; nPass++ )
				{
					for( var doubleSidedTwoPassModeCounter = 0;
						doubleSidedTwoPassModeCounter < ( doubleSidedTwoPassMode ? 2 : 1 );
						doubleSidedTwoPassModeCounter++ )
					{
						//create pass
						var pass = technique.CreatePass();
						Pass shadowCasterPass = null;

						pass.DepthWrite = DepthWrite;
						pass.DepthCheck = DepthTest;

						bool ambientPass;
						bool lightPass;

						var lightType = RenderLightType.Directional;

						if( projectiveTexturing )
							SubscribePassToRenderObjectPassEvent( pass );

						if( Lighting )
						{
							if( mergeAmbientAndDirectionalLightPasses )
							{
								//5 passes. merge ambient and direction light pass to one solid pass.
								//opaque blending only.
								if( Blending != MaterialBlendingTypes.Opaque )
									Log.Fatal( "ShaderBaseMaterial: CreateDefaultTechnique: blending != MaterialBlendingTypes.Opaque." );
								if( passCount != 5 )
									Log.Fatal( "ShaderBaseMaterial: CreateDefaultTechnique: passCount != 5." );

								ambientPass = nPass <= 1;
								lightPass = nPass >= 1;

								switch( nPass )
								{
								case 0:
									//ambient only. this pass skipped when exists directional light.
									pass.SpecialRendering = true;
									pass.SpecialRenderingAllowOnlyNotExistsSpecificLights = true;
									pass.SpecialRenderingLightType = RenderLightType.Directional;
									break;
								case 1:
									//ambient + first directional light
									lightType = RenderLightType.Directional;
									pass.SpecialRendering = true;
									pass.SpecialRenderingAllowOnlyExistsSpecificLights = true;
									pass.SpecialRenderingMaxLightCount = 1;
									pass.SpecialRenderingLightType = lightType;
									break;
								case 2:
									//directional light (ignore first directional light)
									lightType = RenderLightType.Directional;
									pass.SpecialRendering = true;
									pass.SpecialRenderingIteratePerLight = true;
									pass.SpecialRenderingSkipLightCount = 1;
									pass.SpecialRenderingLightType = lightType;
									break;
								case 3:
									//point light
									lightType = RenderLightType.Point;
									pass.SpecialRendering = true;
									pass.SpecialRenderingIteratePerLight = true;
									pass.SpecialRenderingLightType = lightType;
									break;
								case 4:
									//spot light
									lightType = RenderLightType.Spot;
									pass.SpecialRendering = true;
									pass.SpecialRenderingIteratePerLight = true;
									pass.SpecialRenderingLightType = lightType;
									break;
								}
							}
							else
							{
								if( needAmbientPass )
								{
									ambientPass = nPass == 0;
									lightPass = nPass != 0;
									switch( nPass )
									{
									case 1: lightType = RenderLightType.Directional; break;
									case 2: lightType = RenderLightType.Point; break;
									case 3: lightType = RenderLightType.Spot; break;
									}
								}
								else
								{
									ambientPass = false;
									lightPass = true;
									switch( nPass )
									{
									case 0: lightType = RenderLightType.Directional; break;
									case 1: lightType = RenderLightType.Point; break;
									case 2: lightType = RenderLightType.Spot; break;
									}
								}

								if( lightPass )
								{
									pass.SpecialRendering = true;
									pass.SpecialRenderingIteratePerLight = true;
									pass.SpecialRenderingLightType = lightType;
								}

								if( SceneManager.Instance.IsShadowTechniqueStencilBased() )
								{
									if( ambientPass )
										pass.StencilShadowsIlluminationStage = IlluminationStage.Ambient;
									if( lightPass )
										pass.StencilShadowsIlluminationStage = IlluminationStage.PerLight;
								}
							}
						}
						else
						{
							ambientPass = true;
							lightPass = false;
						}

						var lightCount = lightPass ? 1 : 0;

						var needLightmap = lightPass && lightType == RenderLightType.Directional &&
							LightmapTexCoordIndex != -1;

						//create shadow caster material
						if( lightPass && BaseMaterial.GetShadowTextureCasterMaterial( lightType ) == null &&
							SceneManager.Instance.IsShadowTechniqueShadowmapBased() &&
							OnIsNeedSpecialShadowCasterMaterial() )
						{
							var shadowCasterMaterial = MaterialManager.Instance.Create(
								MaterialManager.Instance.GetUniqueName( BaseMaterial.Name + "_ShadowCaster" ) );

							BaseMaterial.SetShadowTextureCasterMaterial( lightType, shadowCasterMaterial );

							var shadowCasterTechnique = shadowCasterMaterial.CreateTechnique();
							shadowCasterPass = shadowCasterTechnique.CreatePass();
							shadowCasterPass.SetFogOverride( FogMode.None, new ColorValue( 0, 0, 0 ), 0, 0, 0 );
						}

						/////////////////////////////////////
						//configure general pass settings
						{
							//disable Direct3D standard fog features
							pass.SetFogOverride( FogMode.None, new ColorValue( 0, 0, 0 ), 0, 0, 0 );

							//configure depth writing flag and blending factors
							switch( Blending )
							{
							case MaterialBlendingTypes.Opaque:
								if( !ambientPass )
								{
									pass.DepthWrite = false;
									pass.SourceBlendFactor = SceneBlendFactor.One;
									pass.DestBlendFactor = SceneBlendFactor.One;
								}
								break;

							case MaterialBlendingTypes.AlphaAdd:
								pass.DepthWrite = false;
								pass.SourceBlendFactor = SceneBlendFactor.SourceAlpha;
								pass.DestBlendFactor = SceneBlendFactor.One;
								break;

							case MaterialBlendingTypes.AlphaBlend:
								pass.DepthWrite = false;
								pass.SourceBlendFactor = SceneBlendFactor.SourceAlpha;
								if( Lighting && !ambientPass )
									pass.DestBlendFactor = SceneBlendFactor.One;
								else
									pass.DestBlendFactor = SceneBlendFactor.OneMinusSourceAlpha;
								break;
							}

							//AlphaReject
							pass.AlphaRejectFunction = AlphaRejectFunction;
							pass.AlphaRejectValue = AlphaRejectValue;
							pass.AlphaToCoverage = AlphaToCoverage;
							if( shadowCasterPass != null )
							{
								shadowCasterPass.AlphaRejectFunction = AlphaRejectFunction;
								shadowCasterPass.AlphaRejectValue = AlphaRejectValue;
							}

							//DoubleSided
							if( DoubleSided )
							{
								if( doubleSidedTwoPassMode )
								{
									pass.CullingMode = doubleSidedTwoPassModeCounter == 0 ?
										CullingMode.Clockwise : CullingMode.Anticlockwise;
								}
								else
									pass.CullingMode = CullingMode.None;

								//shadow caster material
								if( shadowCasterPass != null )
									shadowCasterPass.CullingMode = CullingMode.None;
							}
						}

						/////////////////////////////////////
						//generate general compile arguments and create texture unit states
						var generalArguments = new StringBuilder( 256 );
						var generalSamplerCount = 0;
						var generalTexCoordCount = 4;
						{
							if( RenderSystem.Instance.IsDirect3D() )
								generalArguments.Append( " -DDIRECT3D" );
							if( RenderSystem.Instance.IsOpenGL() )
								generalArguments.Append( " -DOPENGL" );
							if( RenderSystem.Instance.IsOpenGLES() )
								generalArguments.Append( " -DOPENGL_ES" );

							if( lightType == RenderLightType.Directional || lightType == RenderLightType.Spot )
							{
								if( supportAtiHardwareShadows )
									generalArguments.Append( " -DATI_HARDWARE_SHADOWS" );
								if( supportNvidiaHardwareShadows )
									generalArguments.Append( " -DNVIDIA_HARDWARE_SHADOWS" );
							}

							if( ambientPass )
								generalArguments.Append( " -DAMBIENT_PASS" );
							generalArguments.AppendFormat( " -DLIGHT_COUNT={0}", lightCount );
							if( Lighting )
							{
								generalArguments.Append( " -DLIGHTING" );
								if( AmbientLighting )
									generalArguments.Append( " -DAMBIENT_LIGHTING" );
							}
							if( lightPass )
								generalArguments.AppendFormat( " -DLIGHTTYPE_{0}", lightType.ToString().ToUpper() );
							if( DoubleSided )
							{
								generalArguments.Append( " -DDOUBLE_SIDED" );
								if( doubleSidedTwoPassMode && doubleSidedTwoPassModeCounter == 1 )
									generalArguments.Append( " -DDOUBLE_SIDED_TWO_PASS_MODE_BACK_FACE" );
							}
							if( UseNormals )
								generalArguments.Append( " -DUSE_NORMALS" );

							generalArguments.AppendFormat( " -DBLENDING_{0}", Blending.ToString().ToUpper() );

							if( pass.DepthWrite )
								generalArguments.Append( " -DDEPTH_WRITE" );

							if( HalfLambert )
								generalArguments.Append( " -DHALF_LAMBERT" );

							if( depthOffset != 0 )
								generalArguments.Append( " -DDEPTH_OFFSET" );

							//hardware instancing
							if( RenderSystem.Instance.HasShaderModel3() &&
								RenderSystem.Instance.Capabilities.HardwareInstancing )
							{
								var reflectionDynamicCubemap = false;
								if( ( ReflectionColor != new ColorValue( 0, 0, 0 ) && ReflectionPower != 0 ) ||
									ReflectionScaleDynamic )
								{
									if( string.IsNullOrEmpty( ReflectionSpecificCubemap ) )
										reflectionDynamicCubemap = true;
								}

								if( Blending == MaterialBlendingTypes.Opaque && !reflectionDynamicCubemap )
								{
									pass.SupportHardwareInstancing = true;

									generalArguments.Append( " -DINSTANCING" );

									if( shadowCasterPass != null )
										shadowCasterPass.SupportHardwareInstancing = true;
								}
							}

							//Fog
							var fogMode = SceneManager.Instance.GetFogMode();
							if( AllowFog && fogMode != FogMode.None )
							{
								generalArguments.Append( " -DFOG_ENABLED" );
								generalArguments.Append( " -DFOG_" + fogMode.ToString().ToUpper() );
							}

							//FadingByDistanceRange
							if( fadingByDistanceRange != new Range( 0, 0 ) )
								generalArguments.Append( " -DFADING_BY_DISTANCE" );

							//alphaRejectFunction for OpenGL ES
							if( RenderSystem.Instance.IsOpenGLES() )
							{
								if( AlphaRejectFunction != CompareFunction.AlwaysPass )
								{
									generalArguments.AppendFormat( " -DALPHA_REJECT_FUNCTION_{0}",
										AlphaRejectFunction.ToString().ToUpper() );
								}
							}

							//TexCoord23
							var useTexCoord23 = false;
							foreach( var map in GetAllMaps() )
							{
								if( !string.IsNullOrEmpty( map.Texture ) &&
									( map.TexCoord == TexCoordIndexes.TexCoord2 || map.TexCoord == TexCoordIndexes.TexCoord3 ) )
								{
									useTexCoord23 = true;
									break;
								}
							}
							if( needLightmap && LightmapTexCoordIndex > 1 )
								useTexCoord23 = true;
							if( useTexCoord23 )
							{
								generalArguments.Append( " -DTEXCOORD23" );
								generalArguments.AppendFormat( " -DTEXCOORD23_TEXCOORD=TEXCOORD{0}",
									generalTexCoordCount );
								generalTexCoordCount++;
							}

							if( projectiveTexturing )
							{
								generalArguments.Append( " -DPROJECTIVE_TEXTURING" );
								generalArguments.AppendFormat( " -DTEXCOORD_PROJECTIVE_TEXCOORD=TEXCOORD{0}",
									generalTexCoordCount );
								generalTexCoordCount++;
							}

							//Diffuse
							{
								if( IsDynamicDiffuseScale() )
								{
									generalArguments.Append( " -DDYNAMIC_DIFFUSE_SCALE" );
								}
								else
								{
									var scale = DiffuseColor *
										new ColorValue( DiffusePower, DiffusePower, DiffusePower, 1 );
									generalArguments.AppendFormat( " -DDIFFUSE_SCALE=half4({0},{1},{2},{3})",
										scale.Red, scale.Green, scale.Blue, scale.Alpha );
								}

								if( DiffuseVertexColor )
								{
									generalArguments.Append( " -DDIFFUSE_VERTEX_COLOR" );
									generalArguments.AppendFormat( " -DVERTEX_COLOR_TEXCOORD=TEXCOORD{0}",
										generalTexCoordCount );
									generalTexCoordCount++;
								}

								for( var mapIndex = 1; mapIndex <= 4; mapIndex++ )
								{
									MapItem map = null;
									switch( mapIndex )
									{
									case 1: map = Diffuse1Map; break;
									case 2: map = Diffuse2Map; break;
									case 3: map = Diffuse3Map; break;
									case 4: map = Diffuse4Map; break;
									}

									if( !string.IsNullOrEmpty( map.Texture ) )
									{
										generalArguments.AppendFormat( " -DDIFFUSE{0}_MAP", mapIndex );
										generalArguments.AppendFormat( " -DDIFFUSE{0}_MAP_REGISTER=s{1}",
											mapIndex, generalSamplerCount );
										generalSamplerCount++;

										if( projectiveTexturing && map.TexCoord == TexCoordIndexes.Projective )
										{
											generalArguments.AppendFormat( " -DDIFFUSE{0}_MAP_PROJECTIVE", mapIndex );
										}
										else
										{
											generalArguments.AppendFormat( " -DDIFFUSE{0}_MAP_TEXCOORD=", mapIndex );
											GenerateTexCoordString( generalArguments, (int)map.TexCoord, map.Transform,
												$"diffuse{mapIndex}MapTransform");
										}

										var state = pass.CreateTextureUnitState(
											loadTextures ? map.GetTextureFullPath() : "" );
										if( map.Clamp )
											state.SetTextureAddressingMode( TextureAddressingMode.Clamp );

										//shadow caster material
										if( shadowCasterPass != null )
										{
											var casterState = shadowCasterPass.CreateTextureUnitState(
												loadTextures ? map.GetTextureFullPath() : "" );
											if( map.Clamp )
												casterState.SetTextureAddressingMode( TextureAddressingMode.Clamp );
										}

										if( mapIndex > 1 )
										{
											//Opaque			= srcColor * 1 + destColor * 0
											//Add				= srcColor * 1 + destColor * 1
											//Modulate		= srcColor * destColor + destColor * 0
											//AlphaBlend	= srcColor * srcColor.a + destColor * (1 - srcColor.a)
											generalArguments.AppendFormat( " -DDIFFUSE{0}_MAP_BLEND=blend{1}",
												mapIndex, ( (DiffuseMapItem)map ).Blending.ToString() );
										}
									}
								}
							}

							//Reflection
							if( materialScheme > MaterialSchemes.Low &&
								( RenderSystem.Instance.IsDirect3D() || RenderSystem.Instance.IsOpenGL() ) )
							{
								if( ( ReflectionColor != new ColorValue( 0, 0, 0 ) && ReflectionPower != 0 ) ||
									ReflectionScaleDynamic )
								{
									generalArguments.Append( " -DREFLECTION" );

									generalArguments.AppendFormat( " -DREFLECTION_TEXCOORD=TEXCOORD{0}",
										generalTexCoordCount );
									generalTexCoordCount++;

									if( IsDynamicReflectionScale() )
									{
										generalArguments.Append( " -DDYNAMIC_REFLECTION_SCALE" );
									}
									else
									{
										var scale = ReflectionColor * ReflectionPower;
										generalArguments.AppendFormat( " -DREFLECTION_SCALE=half3({0},{1},{2})",
											scale.Red, scale.Green, scale.Blue );
									}

									if( !string.IsNullOrEmpty( ReflectionMap.Texture ) )
									{
										generalArguments.Append( " -DREFLECTION_MAP" );
										generalArguments.AppendFormat( " -DREFLECTION_MAP_REGISTER=s{0}",
											generalSamplerCount );
										generalSamplerCount++;

										generalArguments.Append( " -DREFLECTION_MAP_TEXCOORD=" );
										GenerateTexCoordString( generalArguments, (int)ReflectionMap.TexCoord,
											ReflectionMap.Transform, "reflectionMapTransform" );

										var state = pass.CreateTextureUnitState(
											loadTextures ? ReflectionMap.GetTextureFullPath() : "" );
										if( ReflectionMap.Clamp )
											state.SetTextureAddressingMode( TextureAddressingMode.Clamp );
									}

									generalArguments.AppendFormat( " -DREFLECTION_CUBEMAP_REGISTER=s{0}",
										generalSamplerCount );
									generalSamplerCount++;

									var textureState = pass.CreateTextureUnitState();
									textureState.SetTextureAddressingMode( TextureAddressingMode.Clamp );
									if( !string.IsNullOrEmpty( ReflectionSpecificCubemap ) )
									{
										if( loadTextures )
										{
											textureState.SetCubicTextureName(
												ConvertToFullPath( ReflectionSpecificCubemap ), true );
										}
									}
									else
									{
										SubscribePassToRenderObjectPassEvent( pass );

										if( cubemapEventUnitStates == null )
											cubemapEventUnitStates = new List<Pair<Pass, TextureUnitState>>();
										cubemapEventUnitStates.Add(
											new Pair<Pass, TextureUnitState>( pass, textureState ) );
									}
								}
							}

							//Emission
							if( ambientPass )
							{
								if( ( EmissionColor != new ColorValue( 0, 0, 0 ) && EmissionPower != 0 ) ||
									IsDynamicEmissionScale() )
								{
									generalArguments.Append( " -DEMISSION" );

									if( IsDynamicEmissionScale() )
									{
										generalArguments.Append( " -DDYNAMIC_EMISSION_SCALE" );
									}
									else
									{
										var scale = EmissionColor * EmissionPower;
										generalArguments.AppendFormat( " -DEMISSION_SCALE=half3({0},{1},{2})",
											scale.Red, scale.Green, scale.Blue );
									}

									if( !string.IsNullOrEmpty( EmissionMap.Texture ) )
									{
										generalArguments.Append( " -DEMISSION_MAP" );
										generalArguments.AppendFormat( " -DEMISSION_MAP_REGISTER=s{0}",
											generalSamplerCount );
										generalSamplerCount++;

										generalArguments.Append( " -DEMISSION_MAP_TEXCOORD=" );
										GenerateTexCoordString( generalArguments, (int)EmissionMap.TexCoord,
											EmissionMap.Transform, "emissionMapTransform" );

										var state = pass.CreateTextureUnitState(
											loadTextures ? EmissionMap.GetTextureFullPath() : "" );
										if( EmissionMap.Clamp )
											state.SetTextureAddressingMode( TextureAddressingMode.Clamp );
									}
								}
							}

							//Specular
							if( RenderSystem.Instance.HasShaderModel3() && materialScheme > MaterialSchemes.Low )
							{
								if( lightPass )
								{
									if( ( SpecularColor != new ColorValue( 0, 0, 0 ) && SpecularPower != 0 ) ||
										IsDynamicSpecularScaleAndShininess() )
									{
										generalArguments.Append( " -DSPECULAR" );

										if( IsDynamicSpecularScaleAndShininess() )
										{
											generalArguments.Append( " -DDYNAMIC_SPECULAR_SCALE" );
										}
										else
										{
											var scale = SpecularColor * SpecularPower;
											generalArguments.AppendFormat( " -DSPECULAR_SCALE=half3({0},{1},{2})",
												scale.Red, scale.Green, scale.Blue );
										}

										if( !string.IsNullOrEmpty( SpecularMap.Texture ) )
										{
											generalArguments.Append( " -DSPECULAR_MAP" );
											generalArguments.AppendFormat( " -DSPECULAR_MAP_REGISTER=s{0}",
												generalSamplerCount );
											generalSamplerCount++;

											generalArguments.Append( " -DSPECULAR_MAP_TEXCOORD=" );
											GenerateTexCoordString( generalArguments, (int)SpecularMap.TexCoord,
												SpecularMap.Transform, "specularMapTransform" );

											var state = pass.CreateTextureUnitState(
												loadTextures ? SpecularMap.GetTextureFullPath() : "" );
											if( SpecularMap.Clamp )
												state.SetTextureAddressingMode( TextureAddressingMode.Clamp );
										}
									}
								}
							}

							//Translucency
							if( RenderSystem.Instance.HasShaderModel3() && materialScheme > MaterialSchemes.Low )
							{
								if( lightPass )
								{
									if( ( translucencyColor != new ColorValue( 0, 0, 0 ) && translucencyPower != 0 ) ||
										IsDynamicTranslucencyScaleAndClearness() )
									{
										generalArguments.Append( " -DTRANSLUCENCY" );

										if( IsDynamicTranslucencyScaleAndClearness() )
										{
											generalArguments.Append( " -DDYNAMIC_TRANSLUCENCY_SCALE" );
										}
										else
										{
											var scale = TranslucencyColor * TranslucencyPower;
											generalArguments.AppendFormat( " -DTRANSLUCENCY_SCALE_AND_CLEARNESS=half3({0},{1},{2},{3})",
												scale.Red, scale.Green, scale.Blue, TranslucencyClearness );
										}

										if( !string.IsNullOrEmpty( TranslucencyMap.Texture ) )
										{
											generalArguments.Append( " -DTRANSLUCENCY_MAP" );
											generalArguments.AppendFormat( " -DTRANSLUCENCY_MAP_REGISTER=s{0}",
												generalSamplerCount );
											generalSamplerCount++;

											generalArguments.Append( " -DTRANSLUCENCY_MAP_TEXCOORD=" );
											GenerateTexCoordString( generalArguments, (int)TranslucencyMap.TexCoord,
												TranslucencyMap.Transform, "translucencyMapTransform" );

											var state = pass.CreateTextureUnitState(
												loadTextures ? TranslucencyMap.GetTextureFullPath() : "" );
											if( TranslucencyMap.Clamp )
												state.SetTextureAddressingMode( TextureAddressingMode.Clamp );
										}
									}
								}
							}

							//NormalMap
							if( RenderSystem.Instance.HasShaderModel3() && materialScheme > MaterialSchemes.Low &&
								( RenderSystem.Instance.IsDirect3D() || RenderSystem.Instance.IsOpenGL() ) )
							{
								if( !string.IsNullOrEmpty( NormalMap.Texture ) )
								{
									generalArguments.Append( " -DNORMAL_MAP" );

									if( ambientPass )
									{
										generalArguments.AppendFormat(
											" -DAMBIENT_LIGHT_DIRECTION_TEXCOORD=TEXCOORD{0}",
											generalTexCoordCount );
										generalTexCoordCount++;
									}

									generalArguments.AppendFormat( " -DNORMAL_MAP_REGISTER=s{0}",
										generalSamplerCount );
									generalSamplerCount++;

									generalArguments.Append( " -DNORMAL_MAP_TEXCOORD=" );
									GenerateTexCoordString( generalArguments, (int)NormalMap.TexCoord,
										NormalMap.Transform, "normalMapTransform" );

									var state = pass.CreateTextureUnitState(
										loadTextures ? NormalMap.GetTextureFullPath() : "" );
									if( NormalMap.Clamp )
										state.SetTextureAddressingMode( TextureAddressingMode.Clamp );
								}
							}

							//Height
							if( RenderSystem.Instance.HasShaderModel3() && materialScheme > MaterialSchemes.Low &&
								( RenderSystem.Instance.IsDirect3D() || RenderSystem.Instance.IsOpenGL() ) )
							{
								if( !string.IsNullOrEmpty( NormalMap.Texture ) )
								{
									if( !string.IsNullOrEmpty( HeightMap.Texture ) || HeightFromNormalMapAlpha )
									{
										if( HeightFromNormalMapAlpha )
										{
											generalArguments.Append( " -DHEIGHT_FROM_NORMAL_MAP_ALPHA" );
										}
										else
										{
											generalArguments.Append( " -DHEIGHT_MAP" );
											generalArguments.AppendFormat( " -DHEIGHT_MAP_REGISTER=s{0}",
												generalSamplerCount );
											generalSamplerCount++;

											generalArguments.Append( " -DHEIGHT_MAP_TEXCOORD=" );
											GenerateTexCoordString( generalArguments, (int)HeightMap.TexCoord,
												HeightMap.Transform, "heightMapTransform" );

											var state = pass.CreateTextureUnitState(
												loadTextures ? HeightMap.GetTextureFullPath() : "" );
											if( HeightMap.Clamp )
												state.SetTextureAddressingMode( TextureAddressingMode.Clamp );
										}

										var dTechnique = DisplacementTechnique;
										//no ParallaxOcclusionMapping support in OpenGL
										if( ( RenderSystem.Instance.IsOpenGL() || RenderSystem.Instance.IsOpenGLES() ) &&
											dTechnique == DisplacementTechniques.ParallaxOcclusionMapping )
										{
											dTechnique = DisplacementTechniques.ParallaxMapping;
										}
										generalArguments.AppendFormat( " -DDISPLACEMENT_TECHNIQUE_{0}",
											dTechnique.ToString().ToUpper() );
									}
								}
							}

							//Shadow
							if( materialScheme > MaterialSchemes.Low )
							{
								if( lightPass )
								{
									if( SceneManager.Instance.IsShadowTechniqueShadowmapBased() &&
										ReceiveShadows )
									{
										var pssm = SceneManager.Instance.IsShadowTechniquePSSM() &&
											lightType == RenderLightType.Directional;

										generalArguments.Append( " -DSHADOW_MAP" );

										if( !ReceiveSimpleShadows )
										{
											if( RenderSystem.Instance.HasShaderModel3() &&
												( SceneManager.Instance.ShadowTechnique == ShadowTechniques.ShadowmapHigh ||
												SceneManager.Instance.ShadowTechnique == ShadowTechniques.ShadowmapHighPSSM ) )
											{
												generalArguments.Append( " -DSHADOW_MAP_HIGH" );
											}
											else if( RenderSystem.Instance.HasShaderModel3() &&
												( SceneManager.Instance.ShadowTechnique == ShadowTechniques.ShadowmapMedium ||
												SceneManager.Instance.ShadowTechnique == ShadowTechniques.ShadowmapMediumPSSM ) )
											{
												generalArguments.Append( " -DSHADOW_MAP_MEDIUM" );
											}
											else
											{
												generalArguments.Append( " -DSHADOW_MAP_LOW" );
											}
										}
										else
										{
											generalArguments.Append( " -DSHADOW_MAP_LOW" );
										}

										if( pssm )
											generalArguments.Append( " -DSHADOW_PSSM" );

										var shadowMapCount = pssm ? 3 : 1;
										for( var n = 0; n < shadowMapCount; n++ )
										{
											generalArguments.AppendFormat( " -DSHADOW_MAP{0}_REGISTER=s{1}",
												n, generalSamplerCount );
											generalSamplerCount++;

											generalArguments.AppendFormat( " -DSHADOW_UV{0}_TEXCOORD=TEXCOORD{1}",
												n, generalTexCoordCount );
											generalTexCoordCount++;

											var state = pass.CreateTextureUnitState( "" );
											state.ContentType = TextureUnitState.ContentTypes.Shadow;
											state.SetTextureAddressingMode( TextureAddressingMode.Clamp );
											state.SetTextureFiltering( FilterOptions.Point,
												FilterOptions.Point, FilterOptions.None );

											if( lightType == RenderLightType.Directional ||
												lightType == RenderLightType.Spot )
											{
												if( supportAtiHardwareShadows )
													state.Fetch4 = true;

												if( supportNvidiaHardwareShadows )
												{
													state.SetTextureFiltering( FilterOptions.Linear,
														FilterOptions.Linear, FilterOptions.None );
												}
											}
										}
									}
								}
							}

							//Lightmap
							if( needLightmap )
							{
								generalArguments.Append( " -DLIGHTMAP" );

								generalArguments.AppendFormat( " -DLIGHTMAP_REGISTER=s{0}", generalSamplerCount );
								generalSamplerCount++;

								if( LightmapTexCoordIndex > 3 )
								{
									defaultTechniqueErrorString = "LightmapTexCoordIndex > 3 is not supported.";
									return false;
								}

								generalArguments.AppendFormat( " -DLIGHTMAP_TEXCOORD=texCoord{0}",
									LightmapTexCoordIndex );

								var state = pass.CreateTextureUnitState( "" );
								state.ContentType = TextureUnitState.ContentTypes.Lightmap;
							}

							//Soft Particles
							if( RenderSystem.Instance.HasShaderModel3() && materialScheme > MaterialSchemes.Low &&
								RenderSystem.Instance.IsDirect3D() )
							{
								if( SoftParticles )
								{
									generalArguments.Append( " -DSOFT_PARTICLES" );

									generalArguments.AppendFormat( " -DDEPTH_MAP_REGISTER=s{0}", generalSamplerCount );
									generalSamplerCount++;

									var state = pass.CreateTextureUnitState( "" );
									state.ContentType = TextureUnitState.ContentTypes.AdditionalMRT;
									state.AdditionalMRTIndex = 0;
									state.SetTextureAddressingMode( TextureAddressingMode.Clamp );
									state.SetTextureFiltering( FilterOptions.Point,
										FilterOptions.Point, FilterOptions.None );
								}
							}
						}

						//check maximum sampler count
						if( generalSamplerCount > maxSamplerCount )
						{
							defaultTechniqueErrorString =
								$"The limit of amount of textures is exceeded. Need: {generalSamplerCount}, Maximum: {maxSamplerCount}. ({FileName})";
							return false;
						}

						//check maximum texture coordinates count
						if( generalTexCoordCount > maxTexCoordCount )
						{
							defaultTechniqueErrorString =
								$"The limit of amount of texture coordinates is exceeded. Need: {generalTexCoordCount}, " +
								$"Maximum: {maxTexCoordCount}. ({FileName})";
							return false;
						}

						/////////////////////////////////////
						//generate replace strings for program compiling
						var replaceStrings =
							new List<KeyValuePair<string, string>>();
						{
							//extension file includes
							var extensionFileName = OnGetExtensionFileName();
							if( extensionFileName != null )
							{
								var replaceText = $"#include \"Base/Shaders/{extensionFileName}\"";
								replaceStrings.Add( new KeyValuePair<string, string>(
									"_INCLUDE_EXTENSION_FILE", replaceText ) );
							}
							else
							{
								replaceStrings.Add( new KeyValuePair<string, string>(
									"_INCLUDE_EXTENSION_FILE", "" ) );
							}
						}

						OnAddCompileArguments( generalArguments );

						/////////////////////////////////////
						//generate programs

						//generate program for only ambient pass
						if( ambientPass && !lightPass )
						{
							//vertex program
							var vertexProgram = GpuProgramCacheManager.Instance.AddProgram(
								"ShaderBase_Vertex_", GpuProgramType.Vertex, sourceFileMain,
								"main_vp", vertexSyntax, generalArguments.ToString(), replaceStrings,
								out defaultTechniqueErrorString );
							if( vertexProgram == null )
								return false;

							OnSetProgramAutoConstants( vertexProgram.DefaultParameters, 0,
								GpuProgramType.Vertex, false );
							pass.VertexProgramName = vertexProgram.Name;

							//fragment program
							var fragmentProgram = GpuProgramCacheManager.Instance.AddProgram(
								"ShaderBase_Fragment_", GpuProgramType.Fragment, sourceFileMain,
								"main_fp", fragmentSyntax, generalArguments.ToString(), replaceStrings,
								out defaultTechniqueErrorString );
							if( fragmentProgram == null )
								return false;

							OnSetProgramAutoConstants( fragmentProgram.DefaultParameters, 0,
								GpuProgramType.Fragment, false );
							pass.FragmentProgramName = fragmentProgram.Name;
						}

						//generate program for light passes
						if( lightPass )
						{
							var arguments = new StringBuilder( generalArguments.Length + 100 );
							arguments.Append( generalArguments.ToString() );
							var texCoordCount = generalTexCoordCount;

							for( var nLight = 0; nLight < lightCount; nLight++ )
							{
								arguments.AppendFormat(
									" -DOBJECT_LIGHT_DIRECTION_AND_ATTENUATION_{0}_TEXCOORD=TEXCOORD{1}",
									nLight, texCoordCount );
								texCoordCount++;
							}

							//check maximum texture coordinates count
							if( texCoordCount > maxTexCoordCount )
							{
								defaultTechniqueErrorString =
									$"The limit of amount of texture coordinates is exceeded. Need: {texCoordCount}, " +
									$"Maximum: {maxTexCoordCount}. ({FileName})";
								return false;
							}

							//vertex program
							var vertexProgram = GpuProgramCacheManager.Instance.AddProgram(
								"ShaderBase_Vertex_", GpuProgramType.Vertex, sourceFileMain,
								"main_vp", vertexSyntax, arguments.ToString(), replaceStrings,
								out defaultTechniqueErrorString );
							if( vertexProgram == null )
								return false;

							OnSetProgramAutoConstants( vertexProgram.DefaultParameters, lightCount,
								GpuProgramType.Vertex, false );
							pass.VertexProgramName = vertexProgram.Name;

							//fragment program
							var fragmentProgram = GpuProgramCacheManager.Instance.AddProgram(
								"ShaderBase_Fragment_", GpuProgramType.Fragment, sourceFileMain,
								"main_fp", fragmentSyntax, arguments.ToString(), replaceStrings,
								out defaultTechniqueErrorString );
							if( fragmentProgram == null )
								return false;

							OnSetProgramAutoConstants( fragmentProgram.DefaultParameters, lightCount,
								GpuProgramType.Fragment, false );
							pass.FragmentProgramName = fragmentProgram.Name;
						}

						//shadow caster material
						if( shadowCasterPass != null )
						{
							var arguments = new StringBuilder( generalArguments.Length + 40 );
							arguments.Append( generalArguments.ToString() );

							if( !RenderSystem.Instance.IsOpenGLES() )//for OpenGL ES is already defined before.
							{
								if( AlphaRejectFunction != CompareFunction.AlwaysPass )
								{
									arguments.AppendFormat( " -DALPHA_REJECT_FUNCTION_{0}",
										AlphaRejectFunction.ToString().ToUpper() );
								}
							}

							//vertex program
							var vertexProgram = GpuProgramCacheManager.Instance.AddProgram(
								"ShaderBase_ShadowCaster_Vertex_", GpuProgramType.Vertex, sourceFileShadowCaster,
								"shadowCaster_vp", vertexSyntax, arguments.ToString(),
								replaceStrings, out defaultTechniqueErrorString );
							if( vertexProgram == null )
								return false;

							OnSetProgramAutoConstants( vertexProgram.DefaultParameters, 0,
								GpuProgramType.Vertex, true );
							shadowCasterPass.VertexProgramName = vertexProgram.Name;

							//fragment program
							var fragmentProgram = GpuProgramCacheManager.Instance.AddProgram(
								"ShaderBase_ShadowCaster_Fragment_", GpuProgramType.Fragment, sourceFileShadowCaster,
								"shadowCaster_fp", fragmentSyntax, arguments.ToString(),
								replaceStrings, out defaultTechniqueErrorString );
							if( fragmentProgram == null )
								return false;

							OnSetProgramAutoConstants( fragmentProgram.DefaultParameters, 0,
								GpuProgramType.Fragment, true );
							shadowCasterPass.FragmentProgramName = fragmentProgram.Name;
						}

					}//doubleSidedTwoPassModeCounter

				}//nPass
			}//materialScheme, technique

			InitializeAndUpdateDynamicGpuParameters();

			return true;
		}

		void SubscribePassToRenderObjectPassEvent( Pass pass )
		{
			if( subscribedPassesForRenderObjectPass == null )
				subscribedPassesForRenderObjectPass = new List<Pass>();
			if( !subscribedPassesForRenderObjectPass.Contains( pass ) )
			{
				pass.RenderObjectPass += Pass_RenderObjectPass;
				subscribedPassesForRenderObjectPass.Add( pass );
			}
		}

		void UpdateMapTransformForFixedPipeline( MapItem map )
		{
			var states = map.textureUnitStatesForFixedPipeline;
			if( states == null )
				return;

			foreach( var state in states )
			{
				var transform = map.Transform;
				var animation = transform.Animation;

				state.TextureScroll = transform.Scroll;
				state.TextureRotate = transform.Rotate * ( MathFunctions.PI * 2 );

				if( transform.Scale != new Vec2( 1, 1 ) )
				{
					var s = Vec2.Zero;
					if( transform.Scale.X != 0 )
						s.X = 1.0f / transform.Scale.X;
					if( transform.Scale.Y != 0 )
						s.Y = 1.0f / transform.Scale.Y;
					state.TextureScale = s;
					state.TextureScroll -= ( new Vec2( 1, 1 ) - transform.Scale ) / 2;
				}

				//property RotateRound is not supported

				state.SetScrollAnimation( -animation.ScrollSpeed );
				state.SetRotateAnimation( -animation.RotateSpeed );
			}
		}

		void FixedPipelineAddDiffuseMapsToPass( Pass pass )
		{
			for( var mapIndex = 1; mapIndex <= 4; mapIndex++ )
			{
				MapItem map = null;
				switch( mapIndex )
				{
				case 1: map = Diffuse1Map; break;
				case 2: map = Diffuse2Map; break;
				case 3: map = Diffuse3Map; break;
				case 4: map = Diffuse4Map; break;
				}

				if( !string.IsNullOrEmpty( map.Texture ) )
				{
					var state = pass.CreateTextureUnitState(
						map.GetTextureFullPath(), (int)map.TexCoord );
					if( map.Clamp )
						state.SetTextureAddressingMode( TextureAddressingMode.Clamp );
					if( projectiveTexturing && map.TexCoord == TexCoordIndexes.Projective )
						state.SetProjectiveTexturing( projectiveTexturingFrustum );

					if( map.textureUnitStatesForFixedPipeline == null )
						map.textureUnitStatesForFixedPipeline = new List<TextureUnitState>();
					map.textureUnitStatesForFixedPipeline.Add( state );
					UpdateMapTransformForFixedPipeline( map );

					if( mapIndex > 1 && mapIndex < 5 )
					{
						var mapBlending = ( (DiffuseMapItem)map ).Blending;
						switch( mapBlending )
						{
						case DiffuseMapItem.MapBlendingTypes.Add:
							state.SetColorOperation( LayerBlendOperation.Add );
							break;
						case DiffuseMapItem.MapBlendingTypes.Modulate:
							state.SetColorOperation( LayerBlendOperation.Modulate );
							break;
						case DiffuseMapItem.MapBlendingTypes.AlphaBlend:
							state.SetColorOperation( LayerBlendOperation.AlphaBlend );
							break;
						}
					}
				}
			}
		}

		void CreateFixedPipelineTechnique()
		{
			var diffuseScale = DiffuseColor *
				new ColorValue( DiffusePower, DiffusePower, DiffusePower, 1 );

			//ReceiveShadows
			{
				BaseMaterial.ReceiveShadows = ReceiveShadows;

				//disable receiving shadows when alpha function is enabled
				if( AlphaRejectFunction != CompareFunction.AlwaysPass )
				{
					if( SceneManager.Instance.IsShadowTechniqueShadowmapBased() )
						BaseMaterial.ReceiveShadows = false;
				}
			}

			var tecnhique = BaseMaterial.CreateTechnique();


			if( SceneManager.Instance.IsShadowTechniqueStencilBased() )
			{
				//stencil shadows are enabled

				//ambient pass
				if( Blending == MaterialBlendingTypes.Opaque )
				{
					var pass = tecnhique.CreatePass();
					pass.NormalizeNormals = true;

					pass.Ambient = diffuseScale;
					pass.Diffuse = new ColorValue( 0, 0, 0 );
					pass.Specular = new ColorValue( 0, 0, 0 );

					pass.AlphaRejectFunction = AlphaRejectFunction;
					pass.AlphaRejectValue = AlphaRejectValue;
					pass.Lighting = Lighting;
					if( DoubleSided )
						pass.CullingMode = CullingMode.None;

					pass.DepthWrite = DepthWrite;
					pass.DepthCheck = DepthTest;

					if( !AllowFog || Blending == MaterialBlendingTypes.AlphaAdd )
						pass.SetFogOverride( FogMode.None, new ColorValue( 0, 0, 0 ), 0, 0, 0 );

					FixedPipelineAddDiffuseMapsToPass( pass );

					pass.StencilShadowsIlluminationStage = IlluminationStage.Ambient;
				}

				{
					var pass = tecnhique.CreatePass();
					pass.NormalizeNormals = true;

					pass.Ambient = new ColorValue( 0, 0, 0 );
					pass.Diffuse = diffuseScale;
					if( string.IsNullOrEmpty( SpecularMap.Texture ) )
						pass.Specular = SpecularColor * SpecularPower;
					pass.Shininess = SpecularShininess;

					pass.AlphaRejectFunction = AlphaRejectFunction;
					pass.AlphaRejectValue = AlphaRejectValue;
					pass.Lighting = Lighting;
					if( DoubleSided )
						pass.CullingMode = CullingMode.None;

					pass.DepthWrite = false;
					pass.DepthCheck = DepthTest;

					if( !AllowFog || Blending == MaterialBlendingTypes.AlphaAdd )
						pass.SetFogOverride( FogMode.None, new ColorValue( 0, 0, 0 ), 0, 0, 0 );

					pass.SourceBlendFactor = SceneBlendFactor.SourceAlpha;
					pass.DestBlendFactor = SceneBlendFactor.One;

					if( Blending != MaterialBlendingTypes.Opaque )
					{
						switch( Blending )
						{
						case MaterialBlendingTypes.AlphaAdd:
							pass.SourceBlendFactor = SceneBlendFactor.SourceAlpha;
							pass.DestBlendFactor = SceneBlendFactor.One;
							break;
						case MaterialBlendingTypes.AlphaBlend:
							pass.SourceBlendFactor = SceneBlendFactor.SourceAlpha;
							pass.DestBlendFactor = SceneBlendFactor.OneMinusSourceAlpha;
							break;
						}
					}

					FixedPipelineAddDiffuseMapsToPass( pass );

					pass.StencilShadowsIlluminationStage = IlluminationStage.PerLight;
				}

			}
			else
			{
				//stencil shadows are disabled

				var pass = tecnhique.CreatePass();
				pass.NormalizeNormals = true;

				pass.Ambient = diffuseScale;
				pass.Diffuse = diffuseScale;

				if( string.IsNullOrEmpty( SpecularMap.Texture ) )
					pass.Specular = SpecularColor * SpecularPower;
				pass.Shininess = SpecularShininess;
				pass.AlphaRejectFunction = AlphaRejectFunction;
				pass.AlphaRejectValue = AlphaRejectValue;
				pass.Lighting = Lighting;
				if( DoubleSided )
					pass.CullingMode = CullingMode.None;

				pass.DepthWrite = DepthWrite;
				pass.DepthCheck = DepthTest;

				if( !AllowFog || Blending == MaterialBlendingTypes.AlphaAdd )
					pass.SetFogOverride( FogMode.None, new ColorValue( 0, 0, 0 ), 0, 0, 0 );

				if( Blending != MaterialBlendingTypes.Opaque )
				{
					pass.DepthWrite = false;

					switch( Blending )
					{
					case MaterialBlendingTypes.AlphaAdd:
						pass.SourceBlendFactor = SceneBlendFactor.SourceAlpha;
						pass.DestBlendFactor = SceneBlendFactor.One;
						break;
					case MaterialBlendingTypes.AlphaBlend:
						pass.SourceBlendFactor = SceneBlendFactor.SourceAlpha;
						pass.DestBlendFactor = SceneBlendFactor.OneMinusSourceAlpha;
						break;
					}
				}

				FixedPipelineAddDiffuseMapsToPass( pass );
			}

			//pass for emission
			if( ( emissionColor != new ColorValue( 0, 0, 0 ) && emissionPower != 0 ) ||
				EmissionScaleDynamic )
			{
				var pass = tecnhique.CreatePass();
				pass.NormalizeNormals = true;

				pass.Ambient = new ColorValue( 0, 0, 0 );
				pass.SelfIllumination = emissionColor * emissionPower;

				pass.DepthWrite = false;
				pass.DepthCheck = DepthTest;

				pass.SourceBlendFactor = SceneBlendFactor.SourceAlpha;
				pass.DestBlendFactor = SceneBlendFactor.One;

				if( !AllowFog || Blending == MaterialBlendingTypes.AlphaAdd )
					pass.SetFogOverride( FogMode.None, new ColorValue( 0, 0, 0 ), 0, 0, 0 );

				pass.AlphaRejectFunction = AlphaRejectFunction;
				pass.AlphaRejectValue = AlphaRejectValue;

				pass.Lighting = Lighting;
				if( DoubleSided )
					pass.CullingMode = CullingMode.None;

				if( !string.IsNullOrEmpty( EmissionMap.Texture ) )
				{
					var state = pass.CreateTextureUnitState( EmissionMap.GetTextureFullPath() );
					if( EmissionMap.Clamp )
						state.SetTextureAddressingMode( TextureAddressingMode.Clamp );

					if( EmissionMap.textureUnitStatesForFixedPipeline == null )
						EmissionMap.textureUnitStatesForFixedPipeline = new List<TextureUnitState>();
					EmissionMap.textureUnitStatesForFixedPipeline.Add( state );
					UpdateMapTransformForFixedPipeline( EmissionMap );
				}

				if( SceneManager.Instance.IsShadowTechniqueStencilBased() )
					pass.StencilShadowsIlluminationStage = IlluminationStage.Decal;
			}


			fixedPipelineInitialized = true;
		}

		void CreateEmptyMaterial()
		{
			var tecnhique = BaseMaterial.CreateTechnique();
			var pass = tecnhique.CreatePass();

			if( RenderSystem.Instance.HasShaderModel3() )
			{
				var sourceFile = "Base\\Shaders\\ShaderBase_empty.cg_hlsl";

				string vertexSyntax;
				string fragmentSyntax;
				{
					if( RenderSystem.Instance.IsDirect3D() )
					{
						vertexSyntax = "vs_3_0";
						fragmentSyntax = "ps_3_0";
					}
					else if( RenderSystem.Instance.IsOpenGLES() )
					{
						vertexSyntax = "hlsl2glsl";
						fragmentSyntax = "hlsl2glsl";
					}
					else
					{
						vertexSyntax = "arbvp1";
						fragmentSyntax = "arbfp1";
					}
				}

				string error;

				//vertex program
				var vertexProgram = GpuProgramCacheManager.Instance.AddProgram(
					"ShaderBaseEmpty_Vertex_", GpuProgramType.Vertex, sourceFile,
					"main_vp", vertexSyntax, "", out error );
				if( vertexProgram == null )
					Log.Fatal( error );
				vertexProgram.DefaultParameters.SetNamedAutoConstant( "worldViewProjMatrix",
					GpuProgramParameters.AutoConstantType.WorldViewProjMatrix );
				pass.VertexProgramName = vertexProgram.Name;

				//fragment program
				var fragmentProgram = GpuProgramCacheManager.Instance.AddProgram(
					"ShaderBaseEmpty_Fragment_", GpuProgramType.Fragment, sourceFile,
					"main_fp", fragmentSyntax, "", out error );
				if( fragmentProgram == null )
					Log.Fatal( error );
				pass.FragmentProgramName = fragmentProgram.Name;
			}

			emptyMaterialInitialized = true;
		}

		void ClearBaseMaterial()
		{
			if( fixedPipelineInitialized )
			{
				Diffuse1Map.textureUnitStatesForFixedPipeline = null;
				Diffuse2Map.textureUnitStatesForFixedPipeline = null;
				Diffuse3Map.textureUnitStatesForFixedPipeline = null;
				Diffuse4Map.textureUnitStatesForFixedPipeline = null;
				EmissionMap.textureUnitStatesForFixedPipeline = null;
				ReflectionMap.textureUnitStatesForFixedPipeline = null;
				SpecularMap.textureUnitStatesForFixedPipeline = null;
			}

			//destroy shadow caster material
			for( var n = 0; n < 3; n++ )
			{
				var lightType = (RenderLightType)n;

				var shadowCasterMaterial = BaseMaterial.GetShadowTextureCasterMaterial( lightType );
				if( shadowCasterMaterial != null )
				{
					BaseMaterial.SetShadowTextureCasterMaterial( lightType, null );
					shadowCasterMaterial.Dispose();
				}
			}

			if( subscribedPassesForRenderObjectPass != null )
			{
				foreach( var pass in subscribedPassesForRenderObjectPass )
					pass.RenderObjectPass -= Pass_RenderObjectPass;
				subscribedPassesForRenderObjectPass.Clear();
			}

			mapsWithAnimations = null;
			cubemapEventUnitStates = null;

			//clear material
			BaseMaterial.RemoveAllTechniques();

			fixedPipelineInitialized = false;
			emptyMaterialInitialized = false;
		}

		protected override bool OnInitBaseMaterial()
		{
			if( !base.OnInitBaseMaterial() )
				return false;

			if( CreateEmptyMaterialsForFasterStartupInitialization )
			{
				CreateEmptyMaterial();
				return true;
			}

			defaultTechniqueErrorString = null;

			bool shadersIsNotSupported;
			var success = CreateDefaultTechnique( out shadersIsNotSupported );

			if( !success )
			{
				//no fatal error if is the Resource Editor
				if( !shadersIsNotSupported &&
					EngineApp.Instance.ApplicationType != EngineApp.ApplicationTypes.ResourceEditor )
				{
					if( !string.IsNullOrEmpty( defaultTechniqueErrorString ) )
					{
						Log.Fatal( "Cannot create material \"{0}\". {1}", Name,
							defaultTechniqueErrorString );
					}
					return false;
				}

				ClearBaseMaterial();
				CreateFixedPipelineTechnique();
			}

			return true;
		}

		protected override void OnClearBaseMaterial()
		{
			ClearBaseMaterial();
			base.OnClearBaseMaterial();
		}

		void InitializeAndUpdateDynamicGpuParameters()
		{
			//initialize and update gpu parameters
			UpdateDynamicDiffuseScaleGpuParameter();
			UpdateDynamicEmissionScaleGpuParameter();
			UpdateDynamicReflectionScaleGpuParameter();
			UpdateDynamicSpecularScaleAndShininessGpuParameter();
			UpdateDynamicTranslucencyScaleAndClearnessGpuParameter();
			UpdateFadingByDistanceRangeGpuParameter();
			UpdateSoftParticlesFadingLengthGpuParameter();
			UpdateDepthOffsetGpuParameter();
			UpdateHeightScaleGpuParameter();

			InitializeAndUpdateMapTransformGpuParameters( Diffuse1Map );
			InitializeAndUpdateMapTransformGpuParameters( Diffuse2Map );
			InitializeAndUpdateMapTransformGpuParameters( Diffuse3Map );
			InitializeAndUpdateMapTransformGpuParameters( Diffuse4Map );
			InitializeAndUpdateMapTransformGpuParameters( ReflectionMap );
			InitializeAndUpdateMapTransformGpuParameters( EmissionMap );
			InitializeAndUpdateMapTransformGpuParameters( SpecularMap );
			InitializeAndUpdateMapTransformGpuParameters( TranslucencyMap );
			InitializeAndUpdateMapTransformGpuParameters( NormalMap );
			InitializeAndUpdateMapTransformGpuParameters( HeightMap );
		}

		void SetCustomGpuParameter( GpuParameters parameter, Vec4 value, bool vertex, bool fragment,
			bool needForShadowCasterMaterial )
		{
			string parameterAsString = null;

			var materialCount = needForShadowCasterMaterial ? 4 : 1;
			for( var nMaterial = 0; nMaterial < materialCount; nMaterial++ )
			{
				Material material = null;

				switch( nMaterial )
				{
				case 0:
					material = BaseMaterial;
					break;
				case 1:
					material = BaseMaterial.GetShadowTextureCasterMaterial( RenderLightType.Point );
					break;
				case 2:
					material = BaseMaterial.GetShadowTextureCasterMaterial( RenderLightType.Directional );
					break;
				case 3:
					material = BaseMaterial.GetShadowTextureCasterMaterial( RenderLightType.Spot );
					break;
				}

				if( material == null )
					continue;

				foreach( var technique in material.Techniques )
				{
					foreach( var pass in technique.Passes )
					{
						var vertexParameters = pass.VertexProgramParameters;
						var fragmentParameters = pass.FragmentProgramParameters;

						if( vertexParameters != null || fragmentParameters != null )
						{
							if( vertex && vertexParameters != null )
							{
								if( !pass.IsCustomGpuParameterInitialized( (int)parameter ) )
								{
									if( parameterAsString == null )
										parameterAsString = parameter.ToString();
									vertexParameters.SetNamedAutoConstant( parameterAsString,
										GpuProgramParameters.AutoConstantType.Custom, (int)parameter );
								}
							}

							if( fragment && fragmentParameters != null )
							{
								if( !pass.IsCustomGpuParameterInitialized( (int)parameter ) )
								{
									if( parameterAsString == null )
										parameterAsString = parameter.ToString();
									fragmentParameters.SetNamedAutoConstant( parameterAsString,
										GpuProgramParameters.AutoConstantType.Custom, (int)parameter );
								}
							}

							pass.SetCustomGpuParameter( (int)parameter, value );
						}
					}
				}
			}
		}

		bool IsDynamicDiffuseScale()
		{
			return DiffuseScaleDynamic ||
				( diffuseColor != new ColorValue( 0, 0, 0 ) && diffuseColor != new ColorValue( 1, 1, 1 ) ) ||
				diffusePower != 1;
		}

		void UpdateDynamicDiffuseScaleGpuParameter()
		{
			if( IsDynamicDiffuseScale() )
			{
				var scale = DiffuseColor *
					new ColorValue( DiffusePower, DiffusePower, DiffusePower, 1 );
				SetCustomGpuParameter( GpuParameters.dynamicDiffuseScale, scale.ToVec4(), false, true, true );
			}
		}

		bool IsDynamicEmissionScale()
		{
			return EmissionScaleDynamic ||
				( emissionColor != new ColorValue( 0, 0, 0 ) && emissionColor != new ColorValue( 1, 1, 1 ) ) ||
				( emissionPower != 0 && emissionPower != 1 );
		}

		void UpdateDynamicEmissionScaleGpuParameter()
		{
			if( IsDynamicEmissionScale() )
			{
				SetCustomGpuParameter( GpuParameters.dynamicEmissionScale,
					emissionColor.ToVec4() * emissionPower, false, true, false );
			}
		}

		bool IsDynamicReflectionScale()
		{
			return ReflectionScaleDynamic ||
				( reflectionColor != new ColorValue( 0, 0, 0 ) && reflectionColor != new ColorValue( 1, 1, 1 ) ) ||
				( reflectionPower != 0 && reflectionPower != 1 );
		}

		void UpdateDynamicReflectionScaleGpuParameter()
		{
			if( IsDynamicReflectionScale() )
			{
				SetCustomGpuParameter( GpuParameters.dynamicReflectionScale,
					reflectionColor.ToVec4() * reflectionPower, false, true, false );
			}
		}

		bool IsDynamicSpecularScaleAndShininess()
		{
			return SpecularScaleDynamic || specularColor != new ColorValue( 0, 0, 0 );
		}

		void UpdateDynamicSpecularScaleAndShininessGpuParameter()
		{
			if( IsDynamicSpecularScaleAndShininess() )
			{
				var scale = specularColor * specularPower;
				SetCustomGpuParameter( GpuParameters.dynamicSpecularScaleAndShininess,
					new Vec4( scale.Red, scale.Green, scale.Blue, specularShininess ), false, true, false );
			}
		}

		bool IsDynamicTranslucencyScaleAndClearness()
		{
			return TranslucencyDynamic || translucencyColor != new ColorValue( 0, 0, 0 );
		}

		void UpdateDynamicTranslucencyScaleAndClearnessGpuParameter()
		{
			if( IsDynamicTranslucencyScaleAndClearness() )
			{
				var scale = translucencyColor * translucencyPower;
				SetCustomGpuParameter( GpuParameters.translucencyScaleAndClearness,
					new Vec4( scale.Red, scale.Green, scale.Blue, translucencyClearness ), false, true, false );
			}
		}

		void UpdateFadingByDistanceRangeGpuParameter()
		{
			if( fadingByDistanceRange == Range.Zero )
				return;

			var range = fadingByDistanceRange;
			if( range.Maximum < range.Minimum + .01f )
				range.Maximum = range.Minimum + .01f;
			SetCustomGpuParameter( GpuParameters.fadingByDistanceRange,
				new Vec4( range.Minimum, 1.0f / ( range.Maximum - range.Minimum ), 0, 0 ), false, true, false );
		}

		void UpdateSoftParticlesFadingLengthGpuParameter()
		{
			if( SoftParticles )
			{
				SetCustomGpuParameter( GpuParameters.softParticlesFadingLength,
					new Vec4( softParticlesFadingLength, 0, 0, 0 ), false, true, false );
			}
		}

		void UpdateDepthOffsetGpuParameter()
		{
			SetCustomGpuParameter( GpuParameters.depthOffset, new Vec4( depthOffset, 0, 0, 0 ), true, false,
				false );
		}

		void UpdateHeightScaleGpuParameter()
		{
			if( RenderSystem.Instance.HasShaderModel3() && !string.IsNullOrEmpty( NormalMap.Texture ) )
			{
				if( !string.IsNullOrEmpty( HeightMap.Texture ) || HeightFromNormalMapAlpha )
				{
					SetCustomGpuParameter( GpuParameters.heightScale, new Vec4( heightScale, 0, 0, 0 ), false,
						true, false );
				}
			}
		}

		void InitializeAndUpdateMapTransformGpuParameters( MapItem map )
		{
			//subscribe parameters for animation updating via RenderObjectPass event
			if( map.Transform.Animation.IsDataExists() )
			{
				//add map to mapsWithAnimations
				if( mapsWithAnimations == null )
					mapsWithAnimations = new List<MapItem>();
				if( !mapsWithAnimations.Contains( map ) )
					mapsWithAnimations.Add( map );

				foreach( var technique in BaseMaterial.Techniques )
					foreach( var pass in technique.Passes )
						SubscribePassToRenderObjectPassEvent( pass );
			}

			//update parameters
			UpdateMapTransformGpuParameters( map );
		}

		static float GetMapTransformAnimationTime()
		{
			if( mapTransformAnimationTimeLastFrameRenderTime != RendererWorld.Instance.FrameRenderTime )
			{
				mapTransformAnimationTimeLastFrameRenderTime = RendererWorld.Instance.FrameRenderTime;

				if( RendererWorld.Instance.EnableTimeProgress )
					mapTransformAnimationTime += RendererWorld.Instance.FrameRenderTimeStep;
			}

			return mapTransformAnimationTime;
		}

		void UpdateMapTransformGpuParameters( MapItem map )
		{
			var transform = map.Transform;

			if( !transform.IsDataExists() )
				return;

			//calculate scroll and rotate
			var scroll = transform.Scroll;
			var scale = transform.Scale;
			var rotate = transform.Rotate;

			var animation = transform.Animation;
			if( animation.IsDataExists() )
			{
				var time = GetMapTransformAnimationTime();

				var animationScroll = animation.ScrollSpeed * time;

				var round = animation.ScrollRound;
				if( round.X != 0 )
				{
					animationScroll.X =
						MathFunctions.Round( animationScroll.X * ( 1.0f / round.X ) ) * round.X;
				}
				if( round.Y != 0 )
				{
					animationScroll.Y =
						MathFunctions.Round( animationScroll.Y * ( 1.0f / round.Y ) ) * round.Y;
				}

				scroll += animationScroll;
				rotate += animation.RotateSpeed * time;
			}

			scroll.X = scroll.X % 1.0f;
			scroll.Y = scroll.Y % 1.0f;
			rotate = rotate % 1.0f;

			//calculate matrix
			Mat3 matrix;
			{
				if( scale != new Vec2( 1, 1 ) )
					matrix = Mat3.FromScale( new Vec3( scale.X, scale.Y, 1 ) );
				else
					matrix = Mat3.Identity;

				if( rotate != 0 )
				{
					Mat3 m;
					m = new Mat3( 1, 0, -.5f, 0, 1, -.5f, 0, 0, 1 );
					m *= Mat3.FromRotateByZ( rotate * ( MathFunctions.PI * 2 ) );
					m *= new Mat3( 1, 0, .5f, 0, 1, .5f, 0, 0, 1 );
					matrix *= m;
				}

				if( scroll != Vec2.Zero )
					matrix *= new Mat3( 1, 0, scroll.X, 0, 1, scroll.Y, 0, 0, 1 );
			}

			//find gpu parameters
			GpuParameters mulGpuParameter;
			GpuParameters addGpuParameter;
			var needForShadowCasterMaterial = false;
			{
				if( map == Diffuse1Map )
				{
					mulGpuParameter = GpuParameters.diffuse1MapTransformMul;
					addGpuParameter = GpuParameters.diffuse1MapTransformAdd;
					needForShadowCasterMaterial = true;
				}
				else if( map == Diffuse2Map )
				{
					mulGpuParameter = GpuParameters.diffuse2MapTransformMul;
					addGpuParameter = GpuParameters.diffuse2MapTransformAdd;
					needForShadowCasterMaterial = true;
				}
				else if( map == Diffuse3Map )
				{
					mulGpuParameter = GpuParameters.diffuse3MapTransformMul;
					addGpuParameter = GpuParameters.diffuse3MapTransformAdd;
					needForShadowCasterMaterial = true;
				}
				else if( map == Diffuse4Map )
				{
					mulGpuParameter = GpuParameters.diffuse4MapTransformMul;
					addGpuParameter = GpuParameters.diffuse4MapTransformAdd;
					needForShadowCasterMaterial = true;
				}
				else if( map == ReflectionMap )
				{
					mulGpuParameter = GpuParameters.reflectionMapTransformMul;
					addGpuParameter = GpuParameters.reflectionMapTransformAdd;
				}
				else if( map == EmissionMap )
				{
					mulGpuParameter = GpuParameters.emissionMapTransformMul;
					addGpuParameter = GpuParameters.emissionMapTransformAdd;
				}
				else if( map == SpecularMap )
				{
					mulGpuParameter = GpuParameters.specularMapTransformMul;
					addGpuParameter = GpuParameters.specularMapTransformAdd;
				}
				else if( map == TranslucencyMap )
				{
					mulGpuParameter = GpuParameters.translucencyMapTransformMul;
					addGpuParameter = GpuParameters.translucencyMapTransformAdd;
				}
				else if( map == NormalMap )
				{
					mulGpuParameter = GpuParameters.normalMapTransformMul;
					addGpuParameter = GpuParameters.normalMapTransformAdd;
				}
				else if( map == HeightMap )
				{
					mulGpuParameter = GpuParameters.heightMapTransformMul;
					addGpuParameter = GpuParameters.heightMapTransformAdd;
				}
				else
				{
					Log.Fatal( "ShaderBaseMaterial: Internal error (UpdateMapTransformGpuParameters)." );
					return;
				}
			}

			//set parameters
			SetCustomGpuParameter( mulGpuParameter,
				new Vec4( matrix.Item0.X, matrix.Item0.Y, matrix.Item1.X, matrix.Item1.Y ),
				false, true, needForShadowCasterMaterial );
			SetCustomGpuParameter( addGpuParameter, new Vec4( matrix.Item0.Z, matrix.Item1.Z, 0, 0 ),
				false, true, needForShadowCasterMaterial );
		}

		void Pass_RenderObjectPass( Pass pass, Vec3 objectWorldPosition )
		{
			//update cubemap reflection textures
			if( cubemapEventUnitStates != null )
			{
				for( var n = 0; n < cubemapEventUnitStates.Count; n++ )
				{
					var item = cubemapEventUnitStates[ n ];
					if( item.First == pass )
						UpdateReflectionCubemap( item.Second, objectWorldPosition );
				}
			}

			//update maps transform with animations
			if( mapsWithAnimations != null )
			{
				for( var n = 0; n < mapsWithAnimations.Count; n++ )
					UpdateMapTransformGpuParameters( mapsWithAnimations[ n ] );
			}

			//set the matrix for projective texturing
			if( projectiveTexturing )
			{
				var clipSpaceToImageSpaceMatrix = new Mat4(
					0.5f, 0, 0, 0,
					0, -0.5f, 0, 0,
					0, 0, 1, 0,
					0.5f, 0.5f, 0, 1 );
				var matrix = clipSpaceToImageSpaceMatrix * projectiveTexturingFrustum.GetProjectionMatrix() *
					projectiveTexturingFrustum.GetViewMatrix();
				matrix.Transpose();
				SetCustomGpuParameter( GpuParameters.texViewProjImageMatrix0, matrix.Item0, true, false, false );
				SetCustomGpuParameter( GpuParameters.texViewProjImageMatrix1, matrix.Item1, true, false, false );
				SetCustomGpuParameter( GpuParameters.texViewProjImageMatrix2, matrix.Item2, true, false, false );
				SetCustomGpuParameter( GpuParameters.texViewProjImageMatrix3, matrix.Item3, true, false, false );
			}
		}

		void UpdateReflectionCubemap( TextureUnitState textureUnitState, Vec3 objectWorldPosition )
		{
			var textureName = "";

			//get cubemap from CubemapZone's
			if( Map.Instance != null )
			{
				var zone = CubemapZone.GetZoneForPoint( objectWorldPosition, true );
				if( zone != null )
					textureName = zone.GetTextureName();
			}

			//get cubemap from SkyBox
			if( string.IsNullOrEmpty( textureName ) )
				textureName = SceneManager.Instance.GetSkyBoxTextureName();

			//update state
			textureUnitState.SetCubicTextureName( textureName, true );
		}

		protected override void OnFogAndShadowSettingsChanged( bool fogModeChanged, bool shadowTechniqueChanged )
		{
			base.OnFogAndShadowSettingsChanged( fogModeChanged, shadowTechniqueChanged );

			if( IsBaseMaterialInitialized() )
			{
				if( ( AllowFog && fogModeChanged ) || shadowTechniqueChanged )
					UpdateBaseMaterial();
			}
		}

		public bool IsDefaultTechniqueCreated()
		{
			return string.IsNullOrEmpty( defaultTechniqueErrorString );
		}

		protected override void OnGetEditorShowInformation( List<Pair<string, ColorValue>> lines )
		{
			base.OnGetEditorShowInformation( lines );

			if( !IsDefaultTechniqueCreated() )
			{
				var color = new ColorValue( 1, 0, 0 );

				if( lines.Count != 0 )
					lines.Add( new Pair<string, ColorValue>( "", color ) );
				lines.Add( new Pair<string, ColorValue>( "The fallback fixed pipeline technique is used.", color ) );

				var errorStrings = defaultTechniqueErrorString.Split( new[] { '\n' },
					StringSplitOptions.RemoveEmptyEntries );
				foreach( var errorString in errorStrings )
				{
					//skip warnings
					if( !errorString.Contains( ": warning X" ) )
						lines.Add( new Pair<string, ColorValue>( errorString, color ) );
				}
			}
		}

		void PreloadTexture( string textureName, Texture.Type textureType )
		{
			if( !string.IsNullOrEmpty( textureName ) )
			{
				var texture = TextureManager.Instance.Load( ConvertToFullPath( textureName ), textureType );
				texture?.Touch();
			}
		}

		protected override void OnPreloadResources()
		{
			base.OnPreloadResources();

			PreloadTexture( Diffuse1Map.Texture, Texture.Type.Type2D );
			PreloadTexture( Diffuse2Map.Texture, Texture.Type.Type2D );
			PreloadTexture( Diffuse3Map.Texture, Texture.Type.Type2D );
			PreloadTexture( Diffuse4Map.Texture, Texture.Type.Type2D );
			PreloadTexture( ReflectionMap.Texture, Texture.Type.Type2D );
			PreloadTexture( ReflectionSpecificCubemap, Texture.Type.CubeMap );
			PreloadTexture( EmissionMap.Texture, Texture.Type.Type2D );
			PreloadTexture( SpecularMap.Texture, Texture.Type.Type2D );
			PreloadTexture( TranslucencyMap.Texture, Texture.Type.Type2D );
			PreloadTexture( NormalMap.Texture, Texture.Type.Type2D );
			PreloadTexture( HeightMap.Texture, Texture.Type.Type2D );
		}

		string ConvertToFullPath( string path )
		{
			if( string.IsNullOrEmpty( FileName ) )
				return path;
			return RelativePathUtils.ConvertToFullPath( Path.GetDirectoryName( FileName ), path );
		}

		public void SetProjectiveTexturing( bool enabled, RenderFrustum frustum )
		{
			if( enabled && frustum == null )
				Log.Fatal( "ShaderBaseMaterial: SetProjectiveTexturing: enabled && frustum == null." );
			projectiveTexturing = enabled;
			projectiveTexturingFrustum = frustum;
		}

		public MapItem[] GetAllMaps()
		{
			return new[] { Diffuse1Map, Diffuse2Map, Diffuse3Map, Diffuse4Map, ReflectionMap, EmissionMap, SpecularMap, 
				TranslucencyMap, NormalMap, HeightMap };
		}

		public override bool IsSupportsStaticBatching()
		{
			var reflectionDynamicCubemap = false;
			if( ( ReflectionColor != new ColorValue( 0, 0, 0 ) && ReflectionPower != 0 ) || ReflectionScaleDynamic )
			{
				if( string.IsNullOrEmpty( ReflectionSpecificCubemap ) )
					reflectionDynamicCubemap = true;
			}
			if( Blending == MaterialBlendingTypes.Opaque && !reflectionDynamicCubemap )
				return true;
			return false;
		}
	}
}
