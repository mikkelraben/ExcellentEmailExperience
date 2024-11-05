# find the Package.appxmanifest file

import os
import sys
import xml.etree.ElementTree as ET

def find_manifest():
    for root, dirs, files in os.walk('.'):
        for file in files:
            if file == 'Package.appxmanifest':
                return os.path.join(root, file)
    return None

def main():
    manifest = find_manifest()
    if manifest is None:
        print('Could not find Package.appxmanifest')
        sys.exit(1)

    print('Found manifest at', manifest)

    tree = ET.parse(manifest)
    root = tree.getroot()
    identity = root.find('{http://schemas.microsoft.com/appx/manifest/foundation/windows10}Identity')

    # find the version number in the manifest
    version = identity.attrib['Version']


    print('Current version is', version)

    # bump the version number
    major, minor, build, revision = version.split('.')
    build = str(int(build) + 1)
    new_version = '.'.join([major, minor, build, revision])

    print('New version is', new_version)

    with open(manifest, 'r') as file:
        filedata = file.read()

    filedata = filedata.replace(version, new_version)

    # write the manifest back to disk
    with open(manifest, 'w') as file:
        file.write(filedata)

main()