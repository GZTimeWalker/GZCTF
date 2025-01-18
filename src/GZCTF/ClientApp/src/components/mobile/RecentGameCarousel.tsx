import { Carousel, CarouselProps } from '@mantine/carousel'
import { Box } from '@mantine/core'
import Autoplay from 'embla-carousel-autoplay'
import { FC, useRef } from 'react'
import { RecentGameSlide } from '@Components/mobile/RecentGameSlide'
import { BasicGameInfoModel } from '@Api'

interface RecentGameCarouselProps extends CarouselProps {
  games: BasicGameInfoModel[]
}

export const RecentGameCarousel: FC<RecentGameCarouselProps> = ({ games, ...props }) => {
  const autoplay = useRef(Autoplay({ delay: 5000 }))

  return (
    <Box w="100%" mx="auto">
      <Carousel
        loop
        type="container"
        withIndicators
        withControls={false}
        plugins={[autoplay.current]}
        onMouseEnter={autoplay.current.stop}
        onMouseLeave={autoplay.current.reset}
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
