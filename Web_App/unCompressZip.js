const elevatorModelTag = document.querySelector("#elevatorModelTag");
var zip = new JSZip();

async function getGLBFile() {///from zipped file
    var fileLocation = "assets/elevator_final.zip"; 
    console.log('Uncompressing zip file: ' + fileLocation);
    var cacheURL;
    await fetch(fileLocation) ///Loads a file that it is in the same sub-directory as the code
        .then(res => res.blob()) ///cached file
        .then(blob =>{ 
            zip.loadAsync(blob).then(function (zip) {
            
                Object.keys(zip.files).forEach(function (filename) {
                    if(cacheURL == null){
                        console.log('Output: ' +filename);
                        zip.files[filename].async('blob').then(function (fileBlob) {
                            var url = URL.createObjectURL(fileBlob);
                            // console.log(fileBlob);
                            // console.log('Blob URL is ' +String(url)); ///Format is ''blob:http://....:port/...'
                            cacheURL = String(url);  
                            updateSrcs(filename,cacheURL,fileBlob);
                            // elevatorModelTag.src=cacheURL;
                            setTimeout(() => {
                                URL.revokeObjectURL(url); ///remove to avoid leaks
                            }, 100);   
                        });
                    }
                });
            },function() {
                alert('Not a Valid Zip File');
            });
        }
    );
  }

  async function updateSrcs(fileName, cacheURL,fileBlob) {
    if(fileName == "elevator_final/elevator_final.glb"){
      elevatorModelTag.src=cacheURL;
      elevatorModelTag.iosSrc=cacheURL;
    }
  }