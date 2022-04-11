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
        900: '#007f6e',
        800: '#009985',
        700: '#00BFA5',
        600: '#03caab',
        500: '#0ad7af',
        400: '#1DE9B6',
        300: '#25eeba',
        200: '#64FFDA',
        100: '#A7FFEB'
      },
      alert: {
        100: '#FF8A80',
        200: '#FF5252',
        400: '#FF1744',
        700: '#D50000',
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
