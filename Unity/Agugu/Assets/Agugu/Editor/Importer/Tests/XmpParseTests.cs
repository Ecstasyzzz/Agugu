using NUnit.Framework;

namespace Agugu.Editor
{
    public class XmpParseTests
    {
        private const string xmpString =
            @"<?xpacket begin = ""ï»¿"" id=""W5M0MpCehiHzreSzNTczkc9d""?>
<x:xmpmeta xmlns:x=""adobe:ns:meta/"" x:xmptk=""Adobe XMP Core 5.6-c142 79.160924, 2017/07/13-01:06:39        "">
   <rdf:RDF xmlns:rdf=""http://www.w3.org/1999/02/22-rdf-syntax-ns#"">
      <rdf:Description rdf:about=""""
            xmlns:xmp=""http://ns.adobe.com/xap/1.0/""
            xmlns:dc=""http://purl.org/dc/elements/1.1/""
            xmlns:xmpMM=""http://ns.adobe.com/xap/1.0/mm/""
            xmlns:stEvt=""http://ns.adobe.com/xap/1.0/sType/ResourceEvent#""
            xmlns:photoshop=""http://ns.adobe.com/photoshop/1.0/""
            xmlns:agugu=""http://www.agugu.org/"">
         <xmp:CreatorTool>Adobe Photoshop CC(Windows)</xmp:CreatorTool>
         <xmp:CreateDate>2018-02-04T17:29:49+08:00</xmp:CreateDate>
         <xmp:MetadataDate>2018-02-10T15:33:34+08:00</xmp:MetadataDate>
         <xmp:ModifyDate>2018-02-10T15:33:34+08:00</xmp:ModifyDate>
         <dc:format>application/vnd.adobe.photoshop</dc:format>
         <xmpMM:InstanceID>xmp.iid:0e4b403b-b28f-684c-ae8a-3d6acf7ff867</xmpMM:InstanceID>
         <xmpMM:DocumentID>xmp.did:e48e0d59-fd6b-814b-a175-e177473ddfb0</xmpMM:DocumentID>
         <xmpMM:OriginalDocumentID>xmp.did:e48e0d59-fd6b-814b-a175-e177473ddfb0</xmpMM:OriginalDocumentID>
         <xmpMM:History>
            <rdf:Seq>
               <rdf:li rdf:parseType=""Resource"">
                  <stEvt:action>created</stEvt:action>
                  <stEvt:instanceID>xmp.iid:e48e0d59-fd6b-814b-a175-e177473ddfb0</stEvt:instanceID>
                  <stEvt:when>2018-02-04T17:29:49+08:00</stEvt:when>
                  <stEvt:softwareAgent>Adobe Photoshop CC(Windows)</stEvt:softwareAgent>
               </rdf:li>
               <rdf:li rdf:parseType=""Resource"">
                  <stEvt:action>saved</stEvt:action>
                  <stEvt:instanceID>xmp.iid:bbe31dcc-4dca-d240-95b3-8d5baa382140</stEvt:instanceID>
                  <stEvt:when>2018-02-04T17:54:51+08:00</stEvt:when>
                  <stEvt:softwareAgent>Adobe Photoshop CC(Windows)</stEvt:softwareAgent>
                  <stEvt:changed>/</stEvt:changed>
               </rdf:li>
               <rdf:li rdf:parseType=""Resource"">
                  <stEvt:action>saved</stEvt:action>
                  <stEvt:instanceID>xmp.iid:0e4b403b-b28f-684c-ae8a-3d6acf7ff867</stEvt:instanceID>
                  <stEvt:when>2018-02-10T15:33:34+08:00</stEvt:when>
                  <stEvt:softwareAgent>Adobe Photoshop CC(Windows)</stEvt:softwareAgent>
                  <stEvt:changed>/</stEvt:changed>
               </rdf:li>
            </rdf:Seq>
         </xmpMM:History>
         <photoshop:ColorMode>3</photoshop:ColorMode>
         <photoshop:ICCProfile>sRGB IEC61966-2.1</photoshop:ICCProfile>
         <photoshop:TextLayers>
            <rdf:Bag>
               <rdf:li rdf:parseType=""Resource"">
                  <photoshop:LayerName>Dollar</photoshop:LayerName>
                  <photoshop:LayerText>$9,999,999,999</photoshop:LayerText>
               </rdf:li>
               <rdf:li rdf:parseType=""Resource"">
                  <photoshop:LayerName>ButtonCount</photoshop:LayerName>
                  <photoshop:LayerText>9999</photoshop:LayerText>
               </rdf:li>
               <rdf:li rdf:parseType=""Resource"">
                  <photoshop:LayerName>BagPercentage</photoshop:LayerName>
                  <photoshop:LayerText>60%</photoshop:LayerText>
               </rdf:li>
               <rdf:li rdf:parseType=""Resource"">
                  <photoshop:LayerName>Location</photoshop:LayerName>
                  <photoshop:LayerText>Taipei, Taiwan</photoshop:LayerText>
               </rdf:li>
               <rdf:li rdf:parseType=""Resource"">
                  <photoshop:LayerName>UserName</photoshop:LayerName>
                  <photoshop:LayerText>Nokusoooooo</photoshop:LayerText>
               </rdf:li>
            </rdf:Bag>
         </photoshop:TextLayers>
         <agugu:Config rdf:parseType=""Resource"">
            <agugu:Layers>
               <rdf:Bag>
                  <rdf:li rdf:parseType=""Resource"">
                     <agugu:Id>45</agugu:Id>
                     <agugu:Properties rdf:parseType=""Resource"">
                        <agugu:xAnchor>left</agugu:xAnchor>
                        <agugu:yAnchor>top</agugu:yAnchor>
                     </agugu:Properties>
                  </rdf:li>
                  <rdf:li rdf:parseType=""Resource"">
                     <agugu:Id>13</agugu:Id>
                     <agugu:Properties rdf:parseType=""Resource"">
                        <agugu:xAnchor>left</agugu:xAnchor>
                        <agugu:yAnchor>top</agugu:yAnchor>
                     </agugu:Properties>
                  </rdf:li>
                  <rdf:li rdf:parseType=""Resource"">
                     <agugu:Id>14</agugu:Id>
                     <agugu:Properties rdf:parseType=""Resource"">
                        <agugu:xAnchor>center</agugu:xAnchor>
                        <agugu:yAnchor>center</agugu:yAnchor>
                     </agugu:Properties>
                  </rdf:li>
               </rdf:Bag>
            </agugu:Layers>
         </agugu:Config>
      </rdf:Description>
   </rdf:RDF>
</x:xmpmeta>";

        [Test]
        public void XMPParser_ParseDocument_ListElements()
        {
            PsdLayerConfigs playerConfig = PsdParser.ParseXMP(xmpString);

            Assert.True(playerConfig.HasLayerConfig(13));
            Assert.True(playerConfig.HasLayerConfig(14));
            Assert.True(playerConfig.HasLayerConfig(45));
        }
    }
}