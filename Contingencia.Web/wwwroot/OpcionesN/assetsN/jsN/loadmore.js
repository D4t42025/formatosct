$(document).ready(function(){
  $(".hide-blog").slice(0, 3).show();
  if($(".hide-blog:hidden").length == 0) {
    $("#loadMore").css("display","none");
  }
  else {
    if($(".hide-blog").length > 3){
      $("#loadMore").css("display","inline-block");
    }
    else if($(".hide-blog").length <= 3){
      $("#loadMore").css("display","none");
    }
  }
  $("#loadMore").on("click", function(e){
    e.preventDefault();
    $(".hide-blog:hidden").slice(0, 3).slideDown();
    if($(".hide-blog:hidden").length == 0) {
      $("#loadMore").css("display","none");
    }
    else {
      if($(".hide-blog").length > 3){
        $("#loadMore").css("display","inline-block");
      }
      else if($(".hide-blog").length <= 3){
        $("#loadMore").css("display","none");
      }
    }
  });
})

// load more button js

document.getElementById('loadMore').addEventListener('click', function () {
  this.style.display = 'none';

  adjustStructure();
});

window.addEventListener('resize', function (event) {
  adjustStructure();
}, true);

function adjustStructure() {
  var hideBlogOuterWrap = document.querySelector('.hide-blog-outer-wrap');
  if (window.matchMedia("(max-width: 991px)").matches) {
    hideBlogOuterWrap.style.display = 'block';
  } else {
    hideBlogOuterWrap.style.display = 'flex';
  }
}
