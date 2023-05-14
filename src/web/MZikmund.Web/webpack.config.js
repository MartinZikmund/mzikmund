const path = require('path');

module.exports = {
    entry: './Scripts/index.ts',
    module: {
        rules: [
            {
                test: /\.tsx?$/,
                use: 'ts-loader',
                exclude: /node_modules/,
            },
            {

            }
        ],
    },
    resolve: {
        extensions: ['.tsx', '.ts', '.js'],
    },
    output: {
        library: {
            name: 'MZikmund',
            type: 'var'
        },
        filename: 'site.js',
        path: path.resolve(__dirname, './wwwroot/js'),
    }
};
