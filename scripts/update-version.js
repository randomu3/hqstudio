#!/usr/bin/env node
/**
 * Скрипт для обновления версии в .csproj файлах
 * Используется semantic-release для автоматического версионирования
 */

const fs = require('fs');
const path = require('path');

const version = process.argv[2];

if (!version) {
  console.error('Usage: node update-version.js <version>');
  process.exit(1);
}

const csprojFiles = [
  'HQStudio.Desktop/HQStudio.csproj',
  'HQStudio.API/HQStudio.API.csproj'
];

console.log(`Updating version to ${version}...`);

csprojFiles.forEach(file => {
  const filePath = path.join(process.cwd(), file);
  
  if (!fs.existsSync(filePath)) {
    console.warn(`File not found: ${file}`);
    return;
  }
  
  let content = fs.readFileSync(filePath, 'utf8');
  
  // Update Version
  content = content.replace(
    /<Version>.*<\/Version>/,
    `<Version>${version}</Version>`
  );
  
  // Update AssemblyVersion (major.minor.patch.0)
  content = content.replace(
    /<AssemblyVersion>.*<\/AssemblyVersion>/,
    `<AssemblyVersion>${version}.0</AssemblyVersion>`
  );
  
  // Update FileVersion (major.minor.patch.0)
  content = content.replace(
    /<FileVersion>.*<\/FileVersion>/,
    `<FileVersion>${version}.0</FileVersion>`
  );
  
  fs.writeFileSync(filePath, content, 'utf8');
  console.log(`✓ Updated ${file}`);
});

console.log('Version update complete!');
