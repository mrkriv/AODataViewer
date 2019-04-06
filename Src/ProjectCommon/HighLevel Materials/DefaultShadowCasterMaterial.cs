using System.ComponentModel;
using System.Text;
using Engine;
using Engine.Renderer;
using Engine.MathEx;

namespace ProjectCommon
{
	[Browsable( false )]
	public class DefaultShadowCasterMaterial : HighLevelMaterial
	{
		//

		public RenderLightType LightType { get; set; }

		public bool AtiHardwareShadows { get; set; }

		public bool NvidiaHardwareShadows { get; set; }

		protected override void OnClone( HighLevelMaterial sourceMaterial )
		{
			base.OnClone( sourceMaterial );

			var source = (DefaultShadowCasterMaterial)sourceMaterial;
			LightType = source.LightType;
			AtiHardwareShadows = source.AtiHardwareShadows;
			NvidiaHardwareShadows = source.NvidiaHardwareShadows;
		}

		void SetProgramAutoConstants( GpuProgramParameters parameters )
		{
			parameters.SetNamedAutoConstant( "worldMatrix",
				GpuProgramParameters.AutoConstantType.WorldMatrix );
			parameters.SetNamedAutoConstant( "viewProjMatrix",
				GpuProgramParameters.AutoConstantType.ViewProjMatrix );
			parameters.SetNamedAutoConstant( "texelOffsets",
				GpuProgramParameters.AutoConstantType.TexelOffsets );
			parameters.SetNamedAutoConstant( "cameraPosition",
				GpuProgramParameters.AutoConstantType.CameraPosition );
			parameters.SetNamedAutoConstant( "farClipDistance",
				GpuProgramParameters.AutoConstantType.FarClipDistance );

			parameters.SetNamedAutoConstant( "shadowDirectionalLightBias",
				GpuProgramParameters.AutoConstantType.ShadowDirectionalLightBias );
			parameters.SetNamedAutoConstant( "shadowSpotLightBias",
				GpuProgramParameters.AutoConstantType.ShadowSpotLightBias );
			parameters.SetNamedAutoConstant( "shadowPointLightBias",
				GpuProgramParameters.AutoConstantType.ShadowPointLightBias );

			parameters.SetNamedAutoConstant( "instancing",
				GpuProgramParameters.AutoConstantType.Instancing );
		}

		protected override bool OnInitBaseMaterial()
		{
			if( !base.OnInitBaseMaterial() )
				return false;

			var sourceFile = "Base\\Shaders\\DefaultShadowCaster.cg_hlsl";

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

			var technique = BaseMaterial.CreateTechnique();

			var pass = technique.CreatePass();

			pass.SetFogOverride( FogMode.None, new ColorValue( 0, 0, 0 ), 0, 0, 0 );

			//generate general compile arguments
			var arguments = new StringBuilder( 256 );
			{
				if( RenderSystem.Instance.IsDirect3D() )
					arguments.Append( " -DDIRECT3D" );
				if( RenderSystem.Instance.IsOpenGL() )
					arguments.Append( " -DOPENGL" );
				if( RenderSystem.Instance.IsOpenGLES() )
					arguments.Append( " -DOPENGL_ES" );

				arguments.AppendFormat( " -DLIGHTTYPE_{0}", LightType.ToString().ToUpper() );

				if( LightType == RenderLightType.Directional || LightType == RenderLightType.Spot )
				{
					if( AtiHardwareShadows )
						arguments.Append( " -DATI_HARDWARE_SHADOWS" );
					if( NvidiaHardwareShadows )
						arguments.Append( " -DNVIDIA_HARDWARE_SHADOWS" );
				}

				//hardware instancing
				if( RenderSystem.Instance.HasShaderModel3() &&
					RenderSystem.Instance.Capabilities.HardwareInstancing )
				{
					pass.SupportHardwareInstancing = true;
					arguments.Append( " -DINSTANCING" );
				}
			}

			//generate programs
			{
				string error;

				//vertex program
				var vertexProgram = GpuProgramCacheManager.Instance.AddProgram(
					"DefaultShadowCaster_Vertex_", GpuProgramType.Vertex, sourceFile,
					"main_vp", vertexSyntax, arguments.ToString(), out error );
				if( vertexProgram == null )
				{
					Log.Fatal( error );
					return false;
				}

				SetProgramAutoConstants( vertexProgram.DefaultParameters );
				pass.VertexProgramName = vertexProgram.Name;

				//fragment program
				var fragmentProgram = GpuProgramCacheManager.Instance.AddProgram(
					"DefaultShadowCaster_Fragment_", GpuProgramType.Fragment, sourceFile,
					"main_fp", fragmentSyntax, arguments.ToString(), out error );
				if( fragmentProgram == null )
				{
					Log.Fatal( error );
					return false;
				}

				SetProgramAutoConstants( fragmentProgram.DefaultParameters );
				pass.FragmentProgramName = fragmentProgram.Name;
			}

			return true;
		}

		protected override void OnClearBaseMaterial()
		{
			//clear material
			BaseMaterial.RemoveAllTechniques();

			base.OnClearBaseMaterial();
		}

		public override bool IsSupportsStaticBatching()
		{
			return false;
		}
	}
}
