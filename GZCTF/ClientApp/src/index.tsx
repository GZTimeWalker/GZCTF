import { ChakraProvider, extendTheme, withDefaultColorScheme } from '@chakra-ui/react';
import React from 'react';
import ReactDOM from 'react-dom';
import { Provider } from 'react-redux';
import { BrowserRouter } from 'react-router-dom';
import { App } from './App';
import { store } from './redux/store';

const baseUrl = document.getElementsByTagName('base')[0].getAttribute('href') || undefined;
const rootElement = document.getElementById('root');
const theme = extendTheme(
  {
    config: {
      initialColorMode: 'dark'
    },
    colors: {
      gray: {
        100: '#ebebeb',
        200: '#cfcfcf',
        300: '#b3b3b3',
        400: '#969696',
        500: '#7a7a7a',
        600: '#5e5e5e',
        700: '#414141',
        800: '#252525',
        900: '#202020'
      },
      brand: {
        900: '#331405',
        800: '#4c1e07',
        700: '#66280a',
        600: '#80320c',
        500: '#993b0e',
        400: '#b34511',
        300: '#cc4f13',
        200: '#e65916',
        100: '#ff6318'
      }
    },
    styles: {
      global: {
        body: {
          bg: '#252525'
        }
      }
    }
  },
  withDefaultColorScheme({ colorScheme: 'brand' })
);

ReactDOM.render(
  <BrowserRouter basename={baseUrl}>
    <ChakraProvider theme={theme}>
      <Provider store={store}>
        <App />
      </Provider>
    </ChakraProvider>
  </BrowserRouter>,
  rootElement
);
