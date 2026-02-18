function readEntriesPromise(reader) {
  return new Promise((resolve, reject) => {
    reader.readEntries(resolve, reject);
  });
}

function fileEntryToFile(entry) {
  return new Promise((resolve, reject) => {
    entry.file(resolve, reject);
  });
}

async function traverseEntry(entry, files) {
  if (entry.isFile) {
    const file = await fileEntryToFile(entry);
    files.push(file);
  } else if (entry.isDirectory) {
    const reader = entry.createReader();
    let batch;
    do {
      batch = await readEntriesPromise(reader);
      for (const child of batch) {
        await traverseEntry(child, files);
      }
    } while (batch.length > 0);
  }
}

export async function extractFilesFromDrop(dataTransfer) {
  const files = [];

  if (dataTransfer.items && dataTransfer.items[0] && typeof dataTransfer.items[0].webkitGetAsEntry === 'function') {
    const entries = [];
    for (let i = 0; i < dataTransfer.items.length; i++) {
      const entry = dataTransfer.items[i].webkitGetAsEntry();
      if (entry) entries.push(entry);
    }
    for (const entry of entries) {
      await traverseEntry(entry, files);
    }
  } else {
    for (let i = 0; i < dataTransfer.files.length; i++) {
      files.push(dataTransfer.files[i]);
    }
  }

  return files;
}
