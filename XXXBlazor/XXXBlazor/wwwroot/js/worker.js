// wwwroot/js/workers.js
let dotNetReference = null;
const workers = [];
const maxWorkers = navigator.hardwareConcurrency || 4; // 사용 가능한 CPU 코어 수에 따라 조정

// Blazor 앱에서 호출하는 초기화 메서드
window.initializeWorkers = function (dotNetRef) {
    dotNetReference = dotNetRef;

    // 기존 워커 제거
    for (let worker of workers) {
        worker.terminate();
    }
    workers.length = 0;

    // 새 워커 생성
    for (let i = 0; i < maxWorkers; i++) {
        const worker = new Worker('js/fileChunkWorker.js');
        worker.onmessage = function (e) {
            const { chunkIndex, processedData } = e.data;
            dotNetReference.invokeMethodAsync('OnChunkProcessed', chunkIndex, processedData);
        };
        workers.push(worker);
    }

    //console.log(`${maxWorkers}개의 웹 워커가 초기화되었습니다.`);
};

// 청크 처리 요청
window.processChunk = function (chunkIndex, data) {
    // 작업을 라운드로빈 방식으로 워커에 분배

    const worker = workers[chunkIndex % workers.length];
    worker.postMessage({ chunkIndex, data });
};

// 워커 정리
window.disposeWorkers = function () {
    for (let worker of workers) {
        worker.terminate();
    }
    workers.length = 0;
    dotNetReference = null;
};
