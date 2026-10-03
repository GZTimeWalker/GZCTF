import { Carousel, CarouselProps } from '@mantine/carousel'
import { Box } from '@mantine/core'
import Autoplay from 'embla-carousel-autoplay'
import { FC, useState } from 'react'
import { RecentGameSlide } from '@Components/mobile/RecentGameSlide'
import { BasicGameInfoModel } from '@Api'
import '@mantine/carousel/styles.css'

interface RecentGameCarouselProps extends CarouselProps {
  games: BasicGameInfoModel[]
}

export const RecentGameCarousel: FC<RecentGameCarouselProps> = ({ games, ...props }) => {
  // created once per mount; `useState` keeps a stable instance without reading a ref during render
  const [autoplay] = useState(() => Autoplay({ delay: 5000 }))

  return (
    <Box w="100%" mx="auto">
      <Carousel
        type="container"
        withIndicators
        slideGap="md"
        withControls={false}
        plugins={[autoplay]}
        emblaOptions={{
          loop: true,
        }}
        onMouseEnter={autoplay.stop}
        onMouseLeave={autoplay.reset}
        {...props}
      >
        {games.map((game) => (
          <Carousel.Slide key={game.id}>
            <RecentGameSlide game={game} />
          </Carousel.Slide>
        ))}
      </Carousel>
    </Box>
  )
}
