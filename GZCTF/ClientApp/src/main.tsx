import ReactDOM from 'react-dom';
import { BrowserRouter } from 'react-router-dom';
import { App } from './App';
import { MantineProvider } from '@mantine/core';

ReactDOM.render(
  <BrowserRouter basename={import.meta.env.BASE_URL}>
    <MantineProvider
      theme={{
        colorScheme: 'dark',
        colors: {
          gray: [
            '#EBEBEB',
            '#CFCFCF',
            '#B3B3B3',
            '#969696',
            '#7A7A7A',
            '#5E5E5E',
            '#414141',
            '#252525',
            '#202020',
          ],
          brand: [
            '#A7FFEB',
            '#64FFDA',
            '#25EEBA',
            '#1DE9B6',
            '#0AD7AF',
            '#03CAAB',
            '#00BFA5',
            '#009985',
            '#007F6E',
          ],
          alert: [
            '#FF8A80',
            '#FF5252',
            '#FF354b',
            '#FF1744',
            '#f00822',
            '#e2030b',
            '#D50000',
            '#a20000',
            '#800000'],
        },
        primaryColor: 'brand',
      }}
    >
      <App />
    </MantineProvider>
  </BrowserRouter>,
  document.querySelector('#root')
);
