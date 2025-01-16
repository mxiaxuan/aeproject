/*!
* Start Bootstrap - Agency v7.0.12 (https://startbootstrap.com/theme/agency)
* Copyright 2013-2023 Start Bootstrap
* Licensed under MIT (https://github.com/StartBootstrap/startbootstrap-agency/blob/master/LICENSE)
*/
//
// Scripts
// 

window.addEventListener('DOMContentLoaded', event => {

    // Navbar shrink function
    var navbarShrink = function () {
        const navbarCollapsible = document.body.querySelector('#mainNav');
        if (!navbarCollapsible) {
            return;
        }
        if (window.scrollY === 0) {
            navbarCollapsible.classList.remove('navbar-shrink')
        } else {
            navbarCollapsible.classList.add('navbar-shrink')
        }
    };

    // Shrink the navbar 
    navbarShrink();

    // Shrink the navbar when page is scrolled
    document.addEventListener('scroll', navbarShrink);

    // Activate Bootstrap scrollspy on the main nav element
    const mainNav = document.body.querySelector('#mainNav');
    if (mainNav) {
        new bootstrap.ScrollSpy(document.body, {
            target: '#mainNav',
            rootMargin: '0px 0px -40%',
        });
    }

    // Collapse responsive navbar when toggler is visible
    const navbarToggler = document.body.querySelector('.navbar-toggler');
    const responsiveNavItems = [].slice.call(
        document.querySelectorAll('#navbarResponsive .nav-link')
    );
    responsiveNavItems.map(function (responsiveNavItem) {
        responsiveNavItem.addEventListener('click', () => {
            if (window.getComputedStyle(navbarToggler).display !== 'none') {
                navbarToggler.click();
            }
        });
    });

    // 請求專輯資料並顯示在 album-section 內
    fetch('/Albums/AlbumListPartial')
        .then(response => response.text())
        .then(data => {
            var albumSection = document.getElementById("album-section");
            if (albumSection) {
                albumSection.innerHTML = data;

                // 搜尋框事件綁定
                var searchInput = document.getElementById('searchInput');
                if (!searchInput) {
                    console.log('搜尋框未找到');
                    return;
                } else {
                    console.log('搜尋框找到');
                }

                // 綁定搜尋框的輸入事件
                searchInput.addEventListener('input', function () {
                    var searchText = this.value.toLowerCase();
                    console.log('Search Text:', searchText);  // 查看輸入的內容

                    var albumCards = document.querySelectorAll('.album-card');
                    albumCards.forEach(function (card) {
                        var albumName = card.getAttribute('data-album-name');
                        console.log('Album Name:', albumName);  // 查看每張卡片的專輯名稱

                        if (albumName.includes(searchText)) {
                            card.style.display = 'block';
                        } else {
                            card.style.display = 'none';
                        }
                    });
                });
            }
        })
        .catch(error => console.error('Error loading album list:', error));

    // 導覽列點擊時滾動到相應位置
    var albumLink = document.getElementById("album-link");
    if (albumLink) {
        albumLink.addEventListener("click", function (e) {
            e.preventDefault();
            var albumview = document.getElementById("albumview");
            if (albumview) {
                albumview.scrollIntoView({
                    behavior: "smooth"
                });
            }
        });
    }

    // 處理點擊卡片的「更多詳情」按鈕
    $(document).on('click', '.card-link', function (event) {
        event.preventDefault();
        console.log('showModal');

        var albumId = $(this).data('album-id');
        console.log("Album ID: " + albumId);

        fetch(`/Songs/GetSongsByAlbumId?albumId=${albumId}`)
            .then(response => response.json())
            .then(data => {
                var tbodyContent = '';
                data.forEach(song => {
                    tbodyContent += `
                                        <tr>
                                            <td>${song.songName}</td>
                                            <td>${song.duration}</td>
                                            <td>${song.language}</td>
                                            <td>${song.releaseDate}</td>
                                        </tr>
                                    `;
                });

                var modalBody = document.getElementById("modalBody");
                if (modalBody) {
                    modalBody.innerHTML = tbodyContent;
                }
                $('#albumModal').modal('show');
            })
            .catch(error => {
                console.error('Error loading songs:', error);
                var modalBody = document.getElementById("modalBody");
                if (modalBody) {
                    modalBody.innerHTML = '無法載入曲目資料。';
                }
            });
    });

    // 處理返回首頁按鈕的顯示邏輯
    const homeButton = document.getElementById("backToHome");
    const screenHeight = window.innerHeight; // 動態取得螢幕高度

    window.addEventListener("scroll", function () {
        if (homeButton) { // 確保 homeButton 存在
            if (window.scrollY > screenHeight) { // 滑動超過一個螢幕高度時顯示按鈕
                homeButton.classList.add("visible");
            } else {
                homeButton.classList.remove("visible");
            }
        }
    });
});