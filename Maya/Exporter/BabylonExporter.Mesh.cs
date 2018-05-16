﻿using Autodesk.Maya.OpenMaya;
using Autodesk.Maya.OpenMayaAnim;
using BabylonExport.Entities;
using MayaBabylon;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Maya2Babylon
{
    internal partial class BabylonExporter
    {
        private MFnSkinCluster mFnSkinCluster;          // the skin cluster of the mesh/vertices
        private MFnTransform mFnTransform;              // the transform of the mesh
        private MStringArray allMayaInfluenceNames;     // the joint names that influence the mesh (joint with 0 weight included)
        private MDoubleArray allMayaInfluenceWeights;   // the joint weights for the vertex (0 weight included)
        private bool isSkinExportSuccess;               // if the skin of the mesh will be exported

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mDagPath">DAG path to the transform</param>
        /// <param name="babylonScene"></param>
        /// <returns></returns>
        private BabylonNode ExportDummy(MDagPath mDagPath, BabylonScene babylonScene)
        {
            RaiseMessage(mDagPath.partialPathName, 1);
            
            MFnTransform mFnTransform = new MFnTransform(mDagPath);

            Print(mFnTransform, 2, "Print ExportDummy mFnTransform");

            var babylonMesh = new BabylonMesh { name = mFnTransform.name, id = mFnTransform.uuid().asString() };
            babylonMesh.isDummy = true;

            // Position / rotation / scaling / hierarchy
            ExportNode(babylonMesh, mFnTransform, babylonScene);

            // Animations
            ExportNodeAnimation(babylonMesh, mFnTransform);

            babylonScene.MeshesList.Add(babylonMesh);

            return babylonMesh;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mDagPath">DAG path to the transform above mesh</param>
        /// <param name="babylonScene"></param>
        /// <returns></returns>
        private BabylonNode ExportMesh(MDagPath mDagPath, BabylonScene babylonScene)
        {
            RaiseMessage(mDagPath.partialPathName, 1);

            // Transform above mesh
            mFnTransform = new MFnTransform(mDagPath);

            // Mesh direct child of the transform
            // TODO get the original one rather than the modified?
            MFnMesh mFnMesh = null;
            for (uint i = 0; i < mFnTransform.childCount; i++)
            {
                MObject childObject = mFnTransform.child(i);
                if (childObject.apiType == MFn.Type.kMesh)
                {
                    var _mFnMesh = new MFnMesh(childObject);
                    if (!_mFnMesh.isIntermediateObject)
                    {
                        mFnMesh = _mFnMesh;
                    }
                }
            }
            if (mFnMesh == null)
            {
                RaiseError("No mesh found has child of " + mDagPath.fullPathName);
                return null;
            }

            RaiseMessage("mFnMesh.fullPathName=" + mFnMesh.fullPathName, 2);

            // --- prints ---
            #region prints

            Action<MFnDagNode> printMFnDagNode = (MFnDagNode mFnDagNode) =>
           {
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.name=" + mFnDagNode.name, 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.absoluteName=" + mFnDagNode.absoluteName, 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.fullPathName=" + mFnDagNode.fullPathName, 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.partialPathName=" + mFnDagNode.partialPathName, 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.activeColor=" + mFnDagNode.activeColor.toString(), 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.attributeCount=" + mFnDagNode.attributeCount, 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.childCount=" + mFnDagNode.childCount, 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.dormantColor=" + mFnDagNode.dormantColor, 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.hasUniqueName=" + mFnDagNode.hasUniqueName, 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.inUnderWorld=" + mFnDagNode.inUnderWorld, 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.isDefaultNode=" + mFnDagNode.isDefaultNode, 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.isInstanceable=" + mFnDagNode.isInstanceable, 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.isInstanced(true)=" + mFnDagNode.isInstanced(true), 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.isInstanced(false)=" + mFnDagNode.isInstanced(false), 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.isInstanced()=" + mFnDagNode.isInstanced(), 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.instanceCount(true)=" + mFnDagNode.instanceCount(true), 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.instanceCount(false)=" + mFnDagNode.instanceCount(false), 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.isIntermediateObject=" + mFnDagNode.isIntermediateObject, 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.isShared=" + mFnDagNode.isShared, 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.objectColor=" + mFnDagNode.objectColor, 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.parentCount=" + mFnDagNode.parentCount, 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.parentNamespace=" + mFnDagNode.parentNamespace, 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.uuid().asString()=" + mFnDagNode.uuid().asString(), 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.dagRoot().apiType=" + mFnDagNode.dagRoot().apiType, 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.model.equalEqual(mFnDagNode.objectProperty)=" + mFnDagNode.model.equalEqual(mFnDagNode.objectProperty), 3);
               RaiseVerbose("BabylonExporter.Mesh | mFnDagNode.transformationMatrix.toString()=" + mFnDagNode.transformationMatrix.toString(), 3);
           };

            Action<MFnMesh> printMFnMesh = (MFnMesh _mFnMesh) =>
            {
                printMFnDagNode(mFnMesh);
                RaiseVerbose("BabylonExporter.Mesh | _mFnMesh.numVertices=" + _mFnMesh.numVertices, 3);
                RaiseVerbose("BabylonExporter.Mesh | _mFnMesh.numEdges=" + _mFnMesh.numEdges, 3);
                RaiseVerbose("BabylonExporter.Mesh | _mFnMesh.numPolygons=" + _mFnMesh.numPolygons, 3);
                RaiseVerbose("BabylonExporter.Mesh | _mFnMesh.numFaceVertices=" + _mFnMesh.numFaceVertices, 3);
                RaiseVerbose("BabylonExporter.Mesh | _mFnMesh.numNormals=" + _mFnMesh.numNormals, 3);
                RaiseVerbose("BabylonExporter.Mesh | _mFnMesh.numUVSets=" + _mFnMesh.numUVSets, 3);
                RaiseVerbose("BabylonExporter.Mesh | _mFnMesh.numUVsProperty=" + _mFnMesh.numUVsProperty, 3);
                RaiseVerbose("BabylonExporter.Mesh | _mFnMesh.displayColors=" + _mFnMesh.displayColors, 3);
                RaiseVerbose("BabylonExporter.Mesh | _mFnMesh.numColorSets=" + _mFnMesh.numColorSets, 3);
                RaiseVerbose("BabylonExporter.Mesh | _mFnMesh.numColorsProperty=" + _mFnMesh.numColorsProperty, 3);
                RaiseVerbose("BabylonExporter.Mesh | _mFnMesh.currentUVSetName()=" + _mFnMesh.currentUVSetName(), 3);

                var _uvSetNames = new MStringArray();
                mFnMesh.getUVSetNames(_uvSetNames);
                foreach (var uvSetName in _uvSetNames)
                {
                    RaiseVerbose("BabylonExporter.Mesh | uvSetName=" + uvSetName, 3);
                    RaiseVerbose("BabylonExporter.Mesh | mFnMesh.numUVs(uvSetName)=" + mFnMesh.numUVs(uvSetName), 4);
                    MFloatArray us = new MFloatArray();
                    MFloatArray vs = new MFloatArray();
                    mFnMesh.getUVs(us, vs, uvSetName);
                    RaiseVerbose("BabylonExporter.Mesh | us.Count=" + us.Count, 4);
                }
            };

            Action<MFnTransform> printMFnTransform = (MFnTransform _mFnMesh) =>
            {
                printMFnDagNode(mFnMesh);
            };

            RaiseVerbose("BabylonExporter.Mesh | mFnMesh data", 2);
            printMFnMesh(mFnMesh);

            RaiseVerbose("BabylonExporter.Mesh | mFnTransform data", 2);
            printMFnTransform(mFnTransform);

            Print(mFnTransform, 2, "Print ExportMesh mFnTransform");

            Print(mFnMesh, 2, "Print ExportMesh mFnMesh");

            //// Geometry
            //MIntArray triangleCounts = new MIntArray();
            //MIntArray trianglesVertices = new MIntArray();
            //mFnMesh.getTriangles(triangleCounts, trianglesVertices);
            //RaiseVerbose("BabylonExporter.Mesh | triangleCounts.ToArray()=" + triangleCounts.ToArray().toString(), 3);
            //RaiseVerbose("BabylonExporter.Mesh | trianglesVertices.ToArray()=" + trianglesVertices.ToArray().toString(), 3);
            //int[] polygonsVertexCount = new int[mFnMesh.numPolygons];
            //for (int polygonId = 0; polygonId < mFnMesh.numPolygons; polygonId++)
            //{
            //    polygonsVertexCount[polygonId] = mFnMesh.polygonVertexCount(polygonId);
            //}
            //RaiseVerbose("BabylonExporter.Mesh | polygonsVertexCount=" + polygonsVertexCount.toString(), 3);

            ////MFloatPointArray points = new MFloatPointArray();
            ////mFnMesh.getPoints(points);
            ////RaiseVerbose("BabylonExporter.Mesh | points.ToArray()=" + points.ToArray().Select(mFloatPoint => mFloatPoint.toString()), 3);

            ////MFloatVectorArray normals = new MFloatVectorArray();
            ////mFnMesh.getNormals(normals);
            ////RaiseVerbose("BabylonExporter.Mesh | normals.ToArray()=" + normals.ToArray().Select(mFloatPoint => mFloatPoint.toString()), 3);

            //for (int polygonId = 0; polygonId < mFnMesh.numPolygons; polygonId++)
            //{
            //    MIntArray verticesId = new MIntArray();
            //    RaiseVerbose("BabylonExporter.Mesh | polygonId=" + polygonId, 3);

            //    int nbTriangles = triangleCounts[polygonId];
            //    RaiseVerbose("BabylonExporter.Mesh | nbTriangles=" + nbTriangles, 3);

            //    for (int triangleIndex = 0; triangleIndex < triangleCounts[polygonId]; triangleIndex++)
            //    {
            //        RaiseVerbose("BabylonExporter.Mesh | triangleIndex=" + triangleIndex, 3);
            //        int[] triangleVertices = new int[3];
            //        mFnMesh.getPolygonTriangleVertices(polygonId, triangleIndex, triangleVertices);
            //        RaiseVerbose("BabylonExporter.Mesh | triangleVertices=" + triangleVertices.toString(), 3);

            //        foreach (int vertexId in triangleVertices)
            //        {
            //            RaiseVerbose("BabylonExporter.Mesh | vertexId=" + vertexId, 3);
            //            MPoint point = new MPoint();
            //            mFnMesh.getPoint(vertexId, point);
            //            RaiseVerbose("BabylonExporter.Mesh | point=" + point.toString(), 3);

            //            MVector normal = new MVector();
            //            mFnMesh.getFaceVertexNormal(polygonId, vertexId, normal);
            //            RaiseVerbose("BabylonExporter.Mesh | normal=" + normal.toString(), 3);
            //        }
            //    }
            //}

            #endregion

            if (IsMeshExportable(mFnMesh, mDagPath) == false)
            {
                return null;
            }

            var babylonMesh = new BabylonMesh { name = mFnTransform.name, id = mFnTransform.uuid().asString() };

            // Position / rotation / scaling / hierarchy
            ExportNode(babylonMesh, mFnTransform, babylonScene);

            // Misc.
            // TODO - Retreive from Maya
            // TODO - What is the difference between isVisible and visibility?
            // TODO - Fix fatal error: Attempting to save in C:/Users/Fabrice/AppData/Local/Temp/Fabrice.20171205.1613.ma
            //babylonMesh.isVisible = mDagPath.isVisible;
            //babylonMesh.visibility = meshNode.MaxNode.GetVisibility(0, Tools.Forever);
            //babylonMesh.receiveShadows = meshNode.MaxNode.RcvShadows == 1;
            //babylonMesh.applyFog = meshNode.MaxNode.ApplyAtmospherics == 1;

            if (mFnMesh.numPolygons < 1)
            {
                RaiseError($"Mesh {babylonMesh.name} has no face", 2);
            }

            if (mFnMesh.numVertices < 3)
            {
                RaiseError($"Mesh {babylonMesh.name} has not enough vertices", 2);
            }

            if (mFnMesh.numVertices >= 65536)
            {
                RaiseWarning($"Mesh {babylonMesh.name} has more than 65536 vertices which means that it will require specific WebGL extension to be rendered. This may impact portability of your scene on low end devices.", 2);
            }

            // Animations
            ExportNodeAnimation(babylonMesh, mFnTransform);

            // Material
            MObjectArray shaders = new MObjectArray();
            mFnMesh.getConnectedShaders(0, shaders, new MIntArray());
            if (shaders.Count > 0)
            {
                List<MFnDependencyNode> materials = new List<MFnDependencyNode>();
                foreach (MObject shader in shaders)
                {
                    // Retreive material
                    MFnDependencyNode shadingEngine = new MFnDependencyNode(shader);
                    MPlug mPlugSurfaceShader = shadingEngine.findPlug("surfaceShader");
                    MObject materialObject = mPlugSurfaceShader.source.node;
                    MFnDependencyNode material = new MFnDependencyNode(materialObject);

                    materials.Add(material);
                }

                if (shaders.Count == 1)
                {
                    MFnDependencyNode material = materials[0];

                    // Material is referenced by id
                    babylonMesh.materialId = material.uuid().asString();

                    // Register material for export if not already done
                    if (!referencedMaterials.Contains(material, new MFnDependencyNodeEqualityComparer()))
                    {
                        referencedMaterials.Add(material);
                    }
                }
                else
                {
                    // Create a new id for the group of sub materials
                    string uuidMultiMaterial = GetMultimaterialUUID(materials);

                    // Multi material is referenced by id
                    babylonMesh.materialId = uuidMultiMaterial;

                    // Register multi material for export if not already done
                    if (!multiMaterials.ContainsKey(uuidMultiMaterial))
                    {
                        multiMaterials.Add(uuidMultiMaterial, materials);
                    }
                }
            }

            var vertices = new List<GlobalVertex>();
            var indices = new List<int>();

            var uvSetNames = new MStringArray();
            mFnMesh.getUVSetNames(uvSetNames);
            bool[] isUVExportSuccess = new bool[Math.Min(uvSetNames.Count, 2)];
            for (int indexUVSet = 0; indexUVSet < isUVExportSuccess.Length; indexUVSet++)
            {
                isUVExportSuccess[indexUVSet] = true;
            }

            // skin
            if(_exportSkin)
            {
                mFnSkinCluster = getMFnSkinCluster(mFnMesh);
            }
            int maxNbBones = 0;
            if (mFnSkinCluster != null)
            {
                isSkinExportSuccess = true;
                RaiseMessage($"mFnSkinCluster.name | {mFnSkinCluster.name}", 2);
                Print(mFnSkinCluster, 3, $"Print {mFnSkinCluster.name}");

                // Create the bones dictionary<name, index>
                // TODO: create an index for all nodes in the full DAG
                InitIndexByNodeNameDictionary(mFnSkinCluster);

                // Get the joint names that influence this mesh
                allMayaInfluenceNames = new MStringArray();
                MGlobal.executeCommand($"skinCluster -q -influence {mFnTransform.name}",
                                        allMayaInfluenceNames);

                // Convert name to fullPathName to manage duplicates
                ConvertBoneNameToFullPathName(mFnSkinCluster, allMayaInfluenceNames);

                // get the max number of joints acting on a vertex
                int numVertices = mFnMesh.numVertices;
                int maxNumInfluences = 0;

                // Get max influence on a vertex
                for (int index = 0; index < numVertices; index++)
                {
                    MDoubleArray influenceWeights = new MDoubleArray();
                    String command = $"skinPercent -query -value {mFnSkinCluster.name} {mFnTransform.name}.vtx[{index}]";
                    // Get the weight values of all the influences for this vertex
                    MGlobal.executeCommand(command, influenceWeights);
                    
                    int numInfluences = influenceWeights.Count(weight => weight != 0);
                   
                    maxNumInfluences = Math.Max(maxNumInfluences,numInfluences);
                }
                RaiseMessage($"Max influences : {maxNumInfluences}",2);
                if (maxNumInfluences > 8)
                {
                    RaiseWarning($"Too many bones influences per vertex: {maxNumInfluences}. Babylon.js only support up to 8 bones influences per vertex.", 2);
                    RaiseWarning("The result may not be as expected.");
                }
                maxNbBones = Math.Min(maxNumInfluences, 8);

                if (isSkinExportSuccess)
                {
                    babylonMesh.skeletonId = GetSkeletonIndex(mFnSkinCluster);
                }
                else
                {
                    mFnSkinCluster = null;
                }
            }
            // Export tangents if option is checked and mesh have tangents
            bool isTangentExportSuccess = _exportTangents;

            // TODO - color, alpha
            //var hasColor = unskinnedMesh.NumberOfColorVerts > 0;
            //var hasAlpha = unskinnedMesh.GetNumberOfMapVerts(-2) > 0;

            // TODO - Add custom properties
            //var optimizeVertices = false; // meshNode.MaxNode.GetBoolProperty("babylonjs_optimizevertices");
            var optimizeVertices = _optimizeVertices; // global option

            // Compute normals
            var subMeshes = new List<BabylonSubMesh>();
            ExtractGeometry(mFnMesh, vertices, indices, subMeshes, uvSetNames, ref isUVExportSuccess, ref isTangentExportSuccess, optimizeVertices);

            if (vertices.Count >= 65536)
            {
                RaiseWarning($"Mesh {babylonMesh.name} has {vertices.Count} vertices. This may prevent your scene to work on low end devices where 32 bits indice are not supported", 2);

                if (!optimizeVertices)
                {
                    RaiseError("You can try to optimize your object using [Try to optimize vertices] option", 2);
                }
            }

            for (int indexUVSet = 0; indexUVSet < isUVExportSuccess.Length; indexUVSet++)
            {
                string uvSetName = uvSetNames[indexUVSet];
                // If at least one vertex is mapped to an UV coordinate but some have failed to be exported
                if (isUVExportSuccess[indexUVSet] == false && mFnMesh.numUVs(uvSetName) > 0)
                {
                    RaiseWarning($"Failed to export UV set named {uvSetName}. Ensure all vertices are mapped to a UV coordinate.", 2);
                }
            }

            RaiseMessage($"{vertices.Count} vertices, {indices.Count / 3} faces", 2);

            // Buffers
            babylonMesh.positions = vertices.SelectMany(v => v.Position).ToArray();
            babylonMesh.normals = vertices.SelectMany(v => v.Normal).ToArray();

            // export the skin
            if (mFnSkinCluster != null)
            {
                babylonMesh.matricesWeights = vertices.SelectMany(v => v.Weights.ToArray()).ToArray();
                babylonMesh.matricesIndices = vertices.Select(v => v.BonesIndices).ToArray();

                babylonMesh.numBoneInfluencers = maxNbBones;
                if (maxNbBones > 4)
                {
                    babylonMesh.matricesWeightsExtra = vertices.SelectMany(v => v.WeightsExtra != null ? v.WeightsExtra.ToArray() : new[] { 0.0f, 0.0f, 0.0f, 0.0f }).ToArray();
                    babylonMesh.matricesIndicesExtra = vertices.Select(v => v.BonesIndicesExtra).ToArray();
                }
            }

            // Tangent
            if (isTangentExportSuccess)
            {
                babylonMesh.tangents = vertices.SelectMany(v => v.Tangent).ToArray();
            }
            // Color
            string colorSetName;
            mFnMesh.getCurrentColorSetName(out colorSetName);
            if (mFnMesh.numColors(colorSetName) > 0) {
                babylonMesh.colors = vertices.SelectMany(v => v.Color).ToArray();
            }
            // UVs
            if (uvSetNames.Count > 0 && isUVExportSuccess[0])
            {
                
                babylonMesh.uvs = vertices.SelectMany(v => v.UV).ToArray();
            }
            if (uvSetNames.Count > 1 && isUVExportSuccess[1])
            {
                babylonMesh.uvs2 = vertices.SelectMany(v => v.UV2).ToArray();
            }

            babylonMesh.subMeshes = subMeshes.ToArray();

            // Buffers - Indices
            babylonMesh.indices = indices.ToArray();


            babylonScene.MeshesList.Add(babylonMesh);
            RaiseMessage("BabylonExporter.Mesh | done", 2);

            return babylonMesh;
        }

        /// <summary>
        /// Extract ordered indices on a triangle basis
        /// Extract position and normal of each vertex per face
        /// </summary>
        /// <param name="mFnMesh"></param>
        /// <param name="vertices"></param>
        /// <param name="indices"></param>
        /// <param name="subMeshes"></param>
        /// <param name="uvSetNames"></param>
        /// <param name="isUVExportSuccess"></param>
        /// <param name="optimizeVertices"></param>
        private void ExtractGeometry(MFnMesh mFnMesh, List<GlobalVertex> vertices, List<int> indices, List<BabylonSubMesh> subMeshes, MStringArray uvSetNames, ref bool[] isUVExportSuccess, ref bool isTangentExportSuccess, bool optimizeVertices)
        {
            List<GlobalVertex>[] verticesAlreadyExported = null;

            if (optimizeVertices)
            {
                verticesAlreadyExported = new List<GlobalVertex>[mFnMesh.numVertices];
            }

            MIntArray triangleCounts = new MIntArray();
            MIntArray trianglesVertices = new MIntArray();
            mFnMesh.getTriangles(triangleCounts, trianglesVertices);
            
            MObjectArray shaders = new MObjectArray();
            MIntArray faceMatIndices = new MIntArray(); // given a face index => get a shader index
            mFnMesh.getConnectedShaders(0, shaders, faceMatIndices);

            // Export geometry even if an error occured with shaders
            int nbShaders = Math.Max(1, shaders.Count);
            bool checkShader = nbShaders == shaders.Count;
            RaiseVerbose("shaders.Count=" + shaders.Count, 2);

            // For each material of this mesh
            for (int indexShader = 0; indexShader < nbShaders; indexShader++)
            {
                var nbIndicesSubMesh = 0;
                var minVertexIndexSubMesh = int.MaxValue;
                var maxVertexIndexSubMesh = int.MinValue;
                var subMesh = new BabylonSubMesh { indexStart = indices.Count, materialIndex = indexShader };
                
                // For each polygon of this mesh
                for (int polygonId = 0; polygonId < faceMatIndices.Count; polygonId++)
                {
                    if (checkShader && faceMatIndices[polygonId] != indexShader)
                    {
                        continue;
                    }

                    // The object-relative (mesh-relative/global) vertex indices for this face
                    MIntArray polygonVertices = new MIntArray();
                    mFnMesh.getPolygonVertices(polygonId, polygonVertices);

                    // For each triangle of this polygon
                    for (int triangleId = 0; triangleId < triangleCounts[polygonId]; triangleId++)
                    {
                        int[] polygonTriangleVertices = new int[3];
                        mFnMesh.getPolygonTriangleVertices(polygonId, triangleId, polygonTriangleVertices);

                        /*
                         * Switch coordinate system at global level
                         * 
                         * Piece of code kept just in case
                         * See BabylonExporter for more information
                         */
                        //// Inverse winding order to flip faces
                        //var tmp = triangleVertices[1];
                        //triangleVertices[1] = triangleVertices[2];
                        //triangleVertices[2] = tmp;

                        // For each vertex of this triangle (3 vertices per triangle)
                        foreach (int vertexIndexGlobal in polygonTriangleVertices)
                        {
                            // Get the face-relative (local) vertex id
                            int vertexIndexLocal = 0;
                            for (vertexIndexLocal = 0; vertexIndexLocal < polygonVertices.Count - 1; vertexIndexLocal++) // -1 to stop at vertexIndexLocal=2
                            {
                                if (polygonVertices[vertexIndexLocal] == vertexIndexGlobal)
                                {
                                    break;
                                }
                            }

                            GlobalVertex vertex = ExtractVertex(mFnMesh, polygonId, vertexIndexGlobal, vertexIndexLocal, uvSetNames, ref isUVExportSuccess, ref isTangentExportSuccess);

                            // Optimize vertices
                            if (verticesAlreadyExported != null)
                            {
                                if (verticesAlreadyExported[vertexIndexGlobal] != null)
                                {
                                    var index = verticesAlreadyExported[vertexIndexGlobal].IndexOf(vertex);

                                    // If a stored vertex is similar to current vertex
                                    if (index > -1)
                                    {
                                        // Use stored vertex instead of current one
                                        vertex = verticesAlreadyExported[vertexIndexGlobal][index];
                                    }
                                    else
                                    {
                                        vertex.CurrentIndex = vertices.Count;
                                        verticesAlreadyExported[vertexIndexGlobal].Add(vertex);
                                        vertices.Add(vertex);
                                    }
                                }
                                else
                                {
                                    verticesAlreadyExported[vertexIndexGlobal] = new List<GlobalVertex>();

                                    vertex.CurrentIndex = vertices.Count;
                                    verticesAlreadyExported[vertexIndexGlobal].Add(vertex);
                                    vertices.Add(vertex);
                                }
                            }
                            else
                            {
                                vertex.CurrentIndex = vertices.Count;
                                vertices.Add(vertex);
                            }

                            indices.Add(vertex.CurrentIndex);

                            minVertexIndexSubMesh = Math.Min(minVertexIndexSubMesh, vertex.CurrentIndex);
                            maxVertexIndexSubMesh = Math.Max(maxVertexIndexSubMesh, vertex.CurrentIndex);
                            nbIndicesSubMesh++;
                        }
                    }
                }

                if (nbIndicesSubMesh != 0)
                {
                    subMesh.indexCount = nbIndicesSubMesh;
                    subMesh.verticesStart = minVertexIndexSubMesh;
                    subMesh.verticesCount = maxVertexIndexSubMesh - minVertexIndexSubMesh + 1;

                    subMeshes.Add(subMesh);
                }
            }
        }

        /// <summary>
        /// Extract geometry (position, normal, UVs...) for a specific vertex
        /// </summary>
        /// <param name="mFnMesh"></param>
        /// <param name="polygonId">The polygon (face) to examine</param>
        /// <param name="vertexIndexGlobal">The object-relative (mesh-relative/global) vertex index</param>
        /// <param name="vertexIndexLocal">The face-relative (local) vertex id to examine</param>
        /// <param name="uvSetNames"></param>
        /// <param name="isUVExportSuccess"></param>
        /// <returns></returns>
        private GlobalVertex ExtractVertex(MFnMesh mFnMesh, int polygonId, int vertexIndexGlobal, int vertexIndexLocal, MStringArray uvSetNames, ref bool[] isUVExportSuccess, ref bool isTangentExportSuccess)
        {
            MPoint point = new MPoint();
            mFnMesh.getPoint(vertexIndexGlobal, point);

            MVector normal = new MVector();
            mFnMesh.getFaceVertexNormal(polygonId, vertexIndexGlobal, normal);

            // Switch coordinate system at object level
            point.z *= -1;
            normal.z *= -1;

            var vertex = new GlobalVertex
            {
                BaseIndex = vertexIndexGlobal,
                Position = point.toArray(),
                Normal = normal.toArray()
            };

            // Tangent
            if (isTangentExportSuccess)
            {
                try
                {
                    MVector tangent = new MVector();
                    mFnMesh.getFaceVertexTangent(polygonId, vertexIndexGlobal, tangent);

                    // Switch coordinate system at object level
                    tangent.z *= -1;

                    int tangentId = mFnMesh.getTangentId(polygonId, vertexIndexGlobal);
                    bool isRightHandedTangent = mFnMesh.isRightHandedTangent(tangentId);

                    // Invert W to switch to left handed system
                    vertex.Tangent = new float[] { (float)tangent.x, (float)tangent.y, (float)tangent.z, isRightHandedTangent ? -1 : 1 };
                }
                catch
                {
                    // Exception raised when mesh don't have tangents
                    isTangentExportSuccess = false;
                }
            }

            // Color
            int colorIndex;
            string colorSetName;
            float[] defaultColor = new float[] { 1, 1, 1, 0 };
            MColor color = new MColor();
            mFnMesh.getCurrentColorSetName(out colorSetName);

            if (mFnMesh.numColors(colorSetName) > 0)
            {
                //Get the color index
                mFnMesh.getColorIndex(polygonId, vertexIndexLocal, out colorIndex);
                
                //if a color is set
                if (colorIndex != -1)
                {
                    mFnMesh.getColor(colorIndex, color);
                    vertex.Color = color.toArray();
                }
                //else set the default color
                else
                {
                    vertex.Color = defaultColor;
                }
            }

            // UV
            int indexUVSet = 0;
            if (uvSetNames.Count > indexUVSet && isUVExportSuccess[indexUVSet])
            {
                try
                {
                    float u = 0, v = 0;
                    mFnMesh.getPolygonUV(polygonId, vertexIndexLocal, ref u, ref v, uvSetNames[indexUVSet]);
                    vertex.UV = new float[] { u, v };
                }
                catch
                {
                    // An exception is raised when a vertex isn't mapped to an UV coordinate
                    isUVExportSuccess[indexUVSet] = false;
                }
            }
            indexUVSet = 1;
            if (uvSetNames.Count > indexUVSet && isUVExportSuccess[indexUVSet])
            {
                try
                {
                    float u = 0, v = 0;
                    mFnMesh.getPolygonUV(polygonId, vertexIndexLocal, ref u, ref v, uvSetNames[indexUVSet]);
                    vertex.UV2 = new float[] { u, v };
                }
                catch
                {
                    // An exception is raised when a vertex isn't mapped to an UV coordinate
                    isUVExportSuccess[indexUVSet] = false;
                }
            }

            // skin
            if (mFnSkinCluster != null)
            {
                // Filter null weights
                Dictionary<int, double> weightByInfluenceIndex = new Dictionary<int, double>(); // contains the influence indices with weight > 0

                // Export Weights and BonesIndices for the vertex
                // Get the weight values of all the influences for this vertex
                allMayaInfluenceWeights = new MDoubleArray();
                MGlobal.executeCommand($"skinPercent -query -value {mFnSkinCluster.name} {mFnTransform.name}.vtx[{vertexIndexGlobal}]",
                                        allMayaInfluenceWeights);
                allMayaInfluenceWeights.get(out double[] allInfluenceWeights);

                for (int influenceIndex = 0; influenceIndex < allInfluenceWeights.Length; influenceIndex++)
                {
                    double weight = allInfluenceWeights[influenceIndex];

                    if (weight > 0)
                    {
                        try
                        {
                            // add indice and weight in the local lists
                            weightByInfluenceIndex.Add(indexByNodeName[allMayaInfluenceNames[influenceIndex]], weight);
                        }
                        catch
                        {
                            //RaiseError($"{allMayaInfluenceNames[influenceIndex]} is not supported.", 2);
                        }
                    }
                }

                if (weightByInfluenceIndex.Count > 0)
                {
                    // normalize weights => Sum weights == 1
                    double totalWeight = weightByInfluenceIndex.Values.Sum();
                    if (totalWeight != 1)
                    {
                        for (int index = 0; index < weightByInfluenceIndex.Count; index ++)
                        {
                            int influenceIndex = weightByInfluenceIndex.ElementAt(index).Key;

                            weightByInfluenceIndex[influenceIndex] /= totalWeight;
                        }
                    }

                    // decreasing sort
                    Dictionary<int, double> sortedWeightByInfluenceIndex = new Dictionary<int, double>();
                    sortedWeightByInfluenceIndex = weightByInfluenceIndex.OrderByDescending(pair => pair.Value).ToDictionary(pair => pair.Key, pair => pair.Value);
                    weightByInfluenceIndex = sortedWeightByInfluenceIndex;
                    string message = $"{vertexIndexGlobal}: ";

                    // if there are more than 8 bones/influences. Remove the ones with lowest weights because Babylon.js only support up to 8 bones influences per vertex.
                    while (weightByInfluenceIndex.Count > 8)
                    {
                        // get the min weight
                        int indexToRemove = weightByInfluenceIndex.ElementAt(weightByInfluenceIndex.Count - 1).Key;

                        // delete the min
                        weightByInfluenceIndex.Remove(indexToRemove);
                    }
                }


                int bonesCount = indexByNodeName.Count; // number total of bones/influences for the mesh
                float weight0 = 0;
                float weight1 = 0;
                float weight2 = 0;
                float weight3 = 0;
                int bone0 = bonesCount;
                int bone1 = bonesCount;
                int bone2 = bonesCount;
                int bone3 = bonesCount;
                int nbBones = weightByInfluenceIndex.Count; // number of bones/influences for this vertex

                if (nbBones == 0)
                {
                    weight0 = 1.0f;
                    bone0 = bonesCount;
                }

                if (nbBones > 0)
                {
                    bone0 = weightByInfluenceIndex.ElementAt(0).Key;
                    weight0 = (float)weightByInfluenceIndex.ElementAt(0).Value;

                    if (nbBones > 1)
                    {
                        bone1 = weightByInfluenceIndex.ElementAt(1).Key;
                        weight1 = (float)weightByInfluenceIndex.ElementAt(1).Value;

                        if (nbBones > 2)
                        {
                            bone2 = weightByInfluenceIndex.ElementAt(2).Key;
                            weight2 = (float)weightByInfluenceIndex.ElementAt(2).Value;

                            if (nbBones > 3)
                            {
                                bone3 = weightByInfluenceIndex.ElementAt(3).Key;
                                weight3 = (float)weightByInfluenceIndex.ElementAt(3).Value;
                            }
                        }
                    }
                }

                float[] weights = { weight0, weight1, weight2, weight3 };
                vertex.Weights = weights;
                vertex.BonesIndices = (bone3 << 24) | (bone2 << 16) | (bone1 << 8) | bone0;

                if (nbBones > 4)
                {
                    bone0 = weightByInfluenceIndex.ElementAt(4).Key;
                    weight0 = (float)weightByInfluenceIndex.ElementAt(4).Value;
                    weight1 = 0;
                    weight2 = 0;
                    weight3 = 0;

                    if (nbBones > 5)
                    {
                        bone1 = weightByInfluenceIndex.ElementAt(5).Key;
                        weight1 = (float)weightByInfluenceIndex.ElementAt(4).Value;

                        if (nbBones > 6)
                        {
                            bone2 = weightByInfluenceIndex.ElementAt(6).Key;
                            weight2 = (float)weightByInfluenceIndex.ElementAt(4).Value;

                            if (nbBones > 7)
                            {
                                bone3 = weightByInfluenceIndex.ElementAt(7).Key;
                                weight3 = (float)weightByInfluenceIndex.ElementAt(7).Value;
                            }
                        }
                    }

                    float[] weightsExtra = { weight0, weight1, weight2, weight3 };
                    vertex.WeightsExtra = weightsExtra;
                    vertex.BonesIndicesExtra = (bone3 << 24) | (bone2 << 16) | (bone1 << 8) | bone0;
                }
            }
            return vertex;
        }
        
        private void ExportNode(BabylonAbstractMesh babylonAbstractMesh, MFnTransform mFnTransform, BabylonScene babylonScene)
        {
            RaiseVerbose("BabylonExporter.Mesh | ExportNode", 2);

            // Position / rotation / scaling
            ExportTransform(babylonAbstractMesh, mFnTransform);

            // Hierarchy
            ExportHierarchy(babylonAbstractMesh, mFnTransform);
        }

        private void ExportTransform(BabylonAbstractMesh babylonAbstractMesh, MFnTransform mFnTransform)
        {
            // Position / rotation / scaling
            RaiseVerbose("BabylonExporter.Mesh | ExportTransform", 2);
            float[] position = null;
            float[] rotationQuaternion = null;
            float[] rotation = null;
            float[] scaling = null;
            GetTransform(mFnTransform, ref position, ref rotationQuaternion, ref rotation, ref scaling);

            babylonAbstractMesh.position = position;
            if (_exportQuaternionsInsteadOfEulers)
            {
                babylonAbstractMesh.rotationQuaternion = rotationQuaternion;
            }
            else
            {
                babylonAbstractMesh.rotation = rotation;
            }
            babylonAbstractMesh.scaling = scaling;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mFnDagNode">DAG function set of the node (mesh) below the transform</param>
        /// <param name="mDagPath">DAG path of the transform above the node</param>
        /// <returns></returns>
        private bool IsMeshExportable(MFnDagNode mFnDagNode, MDagPath mDagPath)
        {
            return IsNodeExportable(mFnDagNode, mDagPath);
        }

        private MFnSkinCluster getMFnSkinCluster(MFnMesh mFnMesh)
        {
            MFnSkinCluster mFnSkinCluster = null;

            MPlugArray connections = new MPlugArray();
            mFnMesh.getConnections(connections);
            foreach (MPlug connection in connections)
            {
                MObject source = connection.source.node;
                if (source != null)
                {
                    if (source.hasFn(MFn.Type.kSkinClusterFilter))
                    {
                        mFnSkinCluster = new MFnSkinCluster(source);
                    }

                    if ((mFnSkinCluster == null) && (source.hasFn(MFn.Type.kSet) || source.hasFn(MFn.Type.kPolyNormalPerVertex)))
                    {
                        mFnSkinCluster = getMFnSkinCluster(source);
                    }
                }
            }

            return mFnSkinCluster;
        }

        private MFnSkinCluster getMFnSkinCluster(MObject mObject)
        {
            MFnSkinCluster mFnSkinCluster = null;

            MFnDependencyNode mFnDependencyNode = new MFnDependencyNode(mObject);
            MPlugArray connections = new MPlugArray();
            mFnDependencyNode.getConnections(connections);
            for (int index = 0; index < connections.Count && mFnSkinCluster == null; index++)
            {
                MObject source = connections[index].source.node;
                if (source != null && source.hasFn(MFn.Type.kSkinClusterFilter))
                {
                    mFnSkinCluster = new MFnSkinCluster(source);
                }
            }

            return mFnSkinCluster;
        }
    }
}
