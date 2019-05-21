(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../bin"

(**
QConWhat?
========================

Why Quantification
-------
Biological systems are frequently chosen as a target of modelling approaches. Besides measuring the concentration of products and educts of 
biological reactions and their fluxes, it is often inevitable to gain knowledge of absolute protein abundances to gain a comprehensive understanding. 
For this, numerous approaches have been proposed, ranging from immuno-plotting and radioactive-plotting to ultra-sensitive high-throughput 
measurements using mass spectrometry (ms-based proteomics). But even within the field of ms-based proteomics the tastes differ and many 
absolute quantification methods were proposed in recent years. One shown to be very practical was the QConCAT technology (Even so there is still a
significant lack of computational tools!).

Why QConCATs? 
-------
To facilitate a certain throughput (and due to lots of other reasons) proteins are not measured directly in so called "shotgun-ms" scenarious but indirectly.
This is done by digesting the protein with a known protease, yielding predictable peptides. Sadly, these peptides can often not directly be correlated with a
proteins abundance because e.g they can differ in their likelihood to be ionized due do different physico-chemical properties.
One way out of this dilemma is the design of QConCAT proteins. 

How Do QConCATs work? 
-------


*)
#r "QConQuantifier.dll"
open QConQuantifier

Library.hello 0
(**
Some more info
*)
